package network

import (
	"encoding/json"
	"mahjong-server/pkg/engine"
	"testing"
)

func TestEncodeMessage(t *testing.T) {
	payload := ActionDiscardPayload{TileID: 42}
	msgBytes, err := EncodeMessage("action_discard", payload)
	if err != nil {
		t.Fatalf("Gagal encode pesan: %v", err)
	}

	var envelope Message
	if err := json.Unmarshal(msgBytes, &envelope); err != nil {
		t.Fatalf("Gagal unmarshal envelope: %v", err)
	}

	if envelope.Type != "action_discard" {
		t.Errorf("Ekspektasi type action_discard, didapat %s", envelope.Type)
	}

	var decodedPayload ActionDiscardPayload
	if err := json.Unmarshal(envelope.Payload, &decodedPayload); err != nil {
		t.Fatalf("Gagal decode payload: %v", err)
	}

	if decodedPayload.TileID != 42 {
		t.Errorf("Ekspektasi TileID 42, didapat %d", decodedPayload.TileID)
	}
}

func TestRoundEndPayloadEncoding(t *testing.T) {
	scores := map[engine.PlayerSeat]int{
		engine.SeatEast:  140,
		engine.SeatSouth: 0,
		engine.SeatWest:  0,
		engine.SeatNorth: 0,
	}

	payload := RoundEndPayload{
		Result:         "WIN",
		WinnerSeat:     engine.SeatEast,
		WinnerUsername: "Budi",
		WinType:        "Self-Draw",
		ScoreDetails: engine.ScoreBreakdown{
			BaseWinScore:   100,
			WinSourceScore: 30,
			TotalScore:     140,
		},
		UpdatedMatchScores: scores,
		NextRound:          2,
		IsMatchOver:        false,
	}

	msgBytes, err := EncodeMessage("round_end", payload)
	if err != nil {
		t.Fatalf("Gagal encode round_end: %v", err)
	}

	var envelope Message
	if err := json.Unmarshal(msgBytes, &envelope); err != nil {
		t.Fatalf("Gagal parse envelope round_end: %v", err)
	}

	if envelope.Type != "round_end" {
		t.Errorf("Ekspektasi type round_end, didapat %s", envelope.Type)
	}
}
