# ==============================================================================
# Multi-Stage Dockerfile: Mahjong VIP 3D Game Server + Web Client
# Siap untuk Render.com, Railway, Fly.io, dan VPS
# ==============================================================================

# STAGE 1: Build Go Game Server
FROM golang:1.22-alpine AS builder

WORKDIR /app

# Copy dependency files
COPY server/go.mod server/go.sum* ./server/
WORKDIR /app/server
RUN go mod download || true

# Copy all source files
COPY server/ ./

# Compile static binary
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o /app/game-server ./cmd/game-server

# STAGE 2: Production Container
FROM alpine:3.19

RUN apk --no-cache add ca-certificates tzdata

WORKDIR /app

# Copy compiled binary from builder
COPY --from=builder /app/game-server /app/game-server

# Copy static web assets
COPY web-client /app/web-client

# Default port
ENV PORT=8080
EXPOSE 8080

# Run game server
CMD ["/app/game-server"]
