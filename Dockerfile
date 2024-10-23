# set official golang image as base
FROM golang:1.23.2

# set working directory inside the container
WORKDIR /app

# copy go mod files and download dependencies
COPY src/go.mod src/go.sum ./
RUN go mod download

# copy remaining source code
COPY src/ .

# compile the go server binary
RUN go build -o main .

# expose the port the app will run on
EXPOSE 8080

# execute the binary
CMD ["./main"]
