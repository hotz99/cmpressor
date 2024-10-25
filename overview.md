# Video Compression Platform Project Plan

## High-Level Overview
This project involves creating a platform for video compression where users can upload videos. The system allows for different tiers: free users can upload up to 100MB per 24 hours, and paid users can upload up to 1GB per 24 hours. The platform compresses videos to save storage and enables users to download or view their compressed videos. The architecture leverages multiple services, each focusing on specific functionalities such as video processing, compression, storage, and user management.

## Services Breakdown

### 1. Web Client
- **Responsibilities**:
  - Users can upload videos, view the list of their uploaded and compressed videos, and monitor their storage quota.
  - Allows users to switch between free and paid tiers.
- **Tech Stack**: SvelteKit.

### 2. Video Upload and Processing Service
- **Responsibilities**:
  - Accept video uploads, check the user's tier and enforce the upload quota.
  - Forward the video to the compression service for processing.
- **Implementation**: Uses an ASP.NET-based API to accept file uploads and store them in temporary storage.

### 3. Compression Engine Service
- **Responsibilities**:
  - Compress uploaded videos to reduce storage costs.
  - Support multiple compression algorithms to find an optimal balance between quality and storage.
- **Implementation**: A dedicated Rust-based service interfacing with a video compression library.

### 4. Video Storage Service
- **Responsibilities**:
  - Persist compressed videos and store metadata for future reference.
  - Keep track of the last **n** compressed videos per user.
- **Implementation**: Uses a PostgreSQL database for storing video metadata and keeping a reference to the compressed files.

### 5. Video Quota Management Service
- **Responsibilities**:
  - Enforce tier-based upload quotas for free and paid users (100MB vs. 1GB per 24 hours).
  - Notify users when they are nearing their quota limits.
- **Implementation**: Integrated with the user management system and ASP.NET API gateway.

### 6. API Gateway
- **Responsibilities**:
  - Route incoming requests to appropriate services such as video upload, compression, storage, and metadata.
  - Provide secure endpoints for managing user authentication and authorization.
- **Implementation**: ASP.NET-based gateway for routing and API management.

## Quota Management

1. **Free Tier**: Users can upload up to 100MB per 24 hours.
2. **Paid Tier**: Users can upload up to 1GB per 24 hours.
3. **Quota Enforcement**: Implement quota enforcement by tracking user uploads and timestamps.

## Tech Stack

- **Web Client**: SvelteKit.
- **API Gateway**: ASP.NET.
- **Compression Engine**: Rust.
- **Internal Caching**: Redis.
- **Internal Communication Paradigm**: Event-driven architecture with Kafka.
- **Orchestration and Containerization**: Kubernetes and Docker.
- **Database**: PostgreSQL for storing video metadata and compressed versions of the last **n** videos per user.
- **Search and Indexing**: Elasticsearch for indexing metadata for efficient searching and filtering.

## Example Architecture Diagram

1. **User Client** → **API Gateway (ASP.NET)** → **Video Upload and Processing Service** → **Compression Engine (Rust)** → **Storage**
2. **User Client** → **API Gateway (ASP.NET)** → **Video Storage Service** → **Database (PostgreSQL)**
3. **API Gateway** → **Quota Management Service** → **User Management System**

## Key Challenges to Tackle
1. **Efficient Compression**: Develop an algorithm selection strategy that balances quality and storage savings in the Rust-based compression engine.
2. **Quota Management**: Accurately enforce user upload quotas while providing real-time notifications and insights.
3. **Load Management**: Implement caching strategies and efficient file storage to manage highly frequent uploads or downloads.
4. **Scaling and Resource Management**: Use Kubernetes to scale the compression engine and other services independently based on demand.
5. **Security and Authentication**: Implement robust user authentication and access control to safeguard uploaded content.

## Next Steps
1. **Start with the Core Services**: Develop the ASP.NET-based API Gateway, upload processing, video storage, and basic user management.
2. **Build and Test the Compression Engine**: Develop the Rust-based compression engine, experiment with different compression algorithms, and track performance metrics.
3. **Implement Quota Management**: Set up a quota enforcement mechanism and notifications to monitor user uploads.
4. **Simulate High Traffic**: Implement a traffic simulation tool to test system scalability and reliability under high load conditions.
