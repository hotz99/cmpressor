FROM rust:1.83-slim-bullseye

WORKDIR /app

# paths are relative to the build context `.` (cmpressor/)
# a bit messy, but i understand it
COPY ./rust_compressor/src/ /app/rust_compressor/src/
COPY ./rust_compressor/Cargo.toml ./rust_compressor/build.rs /app/rust_compressor/ 
COPY ./rust_compressor/assets/ /app/assets/
COPY ./proto /app/proto/

RUN apt-get update && apt-get install -y protobuf-compiler ffmpeg

WORKDIR /app/rust_compressor/

RUN cargo build --release

CMD ["./target/release/rust_compressor"]
