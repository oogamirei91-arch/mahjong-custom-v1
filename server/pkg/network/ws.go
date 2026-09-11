package network

import (
	"bufio"
	"crypto/sha1"
	"encoding/base64"
	"errors"
	"io"
	"net"
	"net/http"
	"strings"
	"sync"
)

const wsMagicKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"

// WSConn membungkus koneksi TCP mentah menjadi koneksi WebSocket RFC 6455 standar.
type WSConn struct {
	conn    net.Conn
	bufr    *bufio.Reader
	writeMu sync.Mutex
}

// UpgradeToWebSocket melakukan HTTP Handshake RFC 6455 untuk mengubah koneksi HTTP menjadi WebSocket.
func UpgradeToWebSocket(w http.ResponseWriter, r *http.Request) (*WSConn, error) {
	if strings.ToLower(r.Header.Get("Upgrade")) != "websocket" {
		return nil, errors.New("bukan permintaan upgrade websocket")
	}

	key := r.Header.Get("Sec-WebSocket-Key")
	if key == "" {
		return nil, errors.New("Sec-WebSocket-Key tidak ditemukan")
	}

	// Hitung Sec-WebSocket-Accept = Base64(SHA1(Key + MagicKey))
	h := sha1.New()
	h.Write([]byte(key + wsMagicKey))
	accept := base64.StdEncoding.EncodeToString(h.Sum(nil))

	// Hijack koneksi TCP mentah dari HTTP server
	hj, ok := w.(http.Hijacker)
	if !ok {
		return nil, errors.New("server tidak mendukung HTTP hijacking")
	}

	conn, bufrw, err := hj.Hijack()
	if err != nil {
		return nil, err
	}

	// Kirim HTTP 101 Switching Protocols response
	response := "HTTP/1.1 101 Switching Protocols\r\n" +
		"Upgrade: websocket\r\n" +
		"Connection: Upgrade\r\n" +
		"Sec-WebSocket-Accept: " + accept + "\r\n\r\n"

	if _, err := bufrw.WriteString(response); err != nil {
		conn.Close()
		return nil, err
	}
	if err := bufrw.Flush(); err != nil {
		conn.Close()
		return nil, err
	}

	return &WSConn{
		conn: conn,
		bufr: bufrw.Reader,
	}, nil
}

// ReadTextMessage membaca satu frame pesan teks (Opcode 0x1) dari client.
func (c *WSConn) ReadTextMessage() ([]byte, error) {
	for {
		header, err := c.bufr.ReadByte()
		if err != nil {
			return nil, err
		}

		opcode := header & 0x0F
		// Opcode 0x8 = Close Connection
		if opcode == 0x08 {
			return nil, io.EOF
		}

		lengthByte, err := c.bufr.ReadByte()
		if err != nil {
			return nil, err
		}

		isMasked := (lengthByte & 0x80) != 0
		length := int(lengthByte & 0x7F)

		if length == 126 {
			var extLen [2]byte
			if _, err := io.ReadFull(c.bufr, extLen[:]); err != nil {
				return nil, err
			}
			length = int(extLen[0])<<8 | int(extLen[1])
		} else if length == 127 {
			var extLen [8]byte
			if _, err := io.ReadFull(c.bufr, extLen[:]); err != nil {
				return nil, err
			}
			length = int(extLen[4])<<24 | int(extLen[5])<<16 | int(extLen[6])<<8 | int(extLen[7])
		}

		var mask [4]byte
		if isMasked {
			if _, err := io.ReadFull(c.bufr, mask[:]); err != nil {
				return nil, err
			}
		}

		payload := make([]byte, length)
		if _, err := io.ReadFull(c.bufr, payload); err != nil {
			return nil, err
		}

		// Unmask payload dari client
		if isMasked {
			for i := 0; i < length; i++ {
				payload[i] ^= mask[i%4]
			}
		}

		// Kembalikan jika merupakan text frame (0x1)
		if opcode == 0x01 {
			return payload, nil
		}
	}
}

// WriteTextMessage mengirimkan pesan teks UTF-8 ke client dalam format Frame RFC 6455.
func (c *WSConn) WriteTextMessage(msg []byte) error {
	c.writeMu.Lock()
	defer c.writeMu.Unlock()

	var frame []byte
	frame = append(frame, 0x81) // FIN bit = 1, Opcode = 0x1 (Text Frame)

	length := len(msg)
	if length < 126 {
		frame = append(frame, byte(length))
	} else if length <= 65535 {
		frame = append(frame, 126, byte(length>>8), byte(length&0xFF))
	} else {
		frame = append(frame, 127, 0, 0, 0, 0, byte(length>>24), byte((length>>16)&0xFF), byte((length>>8)&0xFF), byte(length&0xFF))
	}

	frame = append(frame, msg...)
	_, err := c.conn.Write(frame)
	return err
}

// Close menutup koneksi TCP di bawahnya.
func (c *WSConn) Close() error {
	return c.conn.Close()
}
