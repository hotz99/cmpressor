centralized env variable resolution by having it in docker-compose, instead of .env files for each service directory


# **JWT-Based Authentication**

- JWT (JSON Web Token) is a compact, self-contained token used for secure information exchange.
- Consists of:
  - **Header**: Metadata like signing algorithm.
  - **Payload**: Claims (e.g., user data).
  - **Signature**: Verifies payload integrity.

---

## **How It Works**
1. **Login**:
   - Server generates a JWT after authenticating the user.
   - JWT is signed using a secret key or private key.
2. **Client**:
   - Stores the JWT (e.g., in cookies or localStorage).
   - Sends the token with requests (`Authorization: Bearer <token>`).
3. **Server**:
   - Validates the token's signature and expiry.
   - Extracts claims from the payload (e.g., user roles).

---

## **Security Notes**
- **Signed Tokens**: JWTs are signed using hashing algorithms to generate a cryptographic signature that ensures the token’s integrity and authenticity. If the token is tampered with (e.g., during transmission), the server detects it because the signature will no longer match the token’s contents, and the token will be rejected.
- **Validation**: Always validate the token's signature, expiry (`exp`), issuer (`iss`), and audience (`aud`).
- **Claims**: Only include non-sensitive data in the payload.
- **Source of Truth**: Use the database for critical data updates.
