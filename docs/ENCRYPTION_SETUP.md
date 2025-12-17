# LCID Encryption Setup

## Secret Manager Configuration

The PlayerDataService now encrypts LCID values using AES-256-GCM. You need to add an encryption key to Unity Secret Manager.

### Step 1: Generate Encryption Key

Generate a secure 256-bit (32-byte) key and encode it as base64:

```bash
# Generate random 32-byte key and encode as base64
openssl rand -base64 32
```

Example output: `Kv8/7V8C2m9LZ4e8F5a7X1b3N6g4Q8k2R9w5T7u1Y3s=`

### Step 2: Add Key to Unity Secret Manager

1. Go to Unity Dashboard → Your Project → Cloud Code → Secret Manager
2. Click "Add Secret"
3. Set Name: `LCID_ENCRYPTION_KEY`
4. Set Value: `<your_base64_encoded_key_from_step_1>`
5. Save the secret

### Important Notes

- The key must be exactly 256 bits (32 bytes) when decoded from base64
- Keep the key secure - losing it will make existing encrypted data unrecoverable
- The same key must be used across all environments for the same project

### Verification

After deployment, the system will:
- Automatically encrypt all new LCID values
- Decrypt existing encrypted values
- Migrate plain text LCID values to encrypted format on first read (lazy migration)
