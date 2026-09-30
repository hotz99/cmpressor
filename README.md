# cmpressor

cmpressor is a video compression platform. Users upload a video, and the platform compresses it. It uses a microservice architecture.

## Services

| Service | Stack | Function |
|---|---|---|
| `sveltekit_server` | SvelteKit | Web client. Users sign in and upload videos. |
| `aspnet_server` | ASP.NET, Dapper | API gateway. Handles users, JWT authentication and uploads. |
| `rust_compressor` | Rust, ffmpeg | Compresses the videos. |
| `rabbitmq` | RabbitMQ | Sends compression requests (Protobuf) from the API to the compressor. |
| `postgres` | PostgreSQL | Stores users and subscription plans. |

`overview.md` describes the full plan, which includes free and paid tiers with upload quotas.

## Setup

1. Copy `.env.example` to `.env`. Set your values.
2. Start all services: `docker compose up --build`.
