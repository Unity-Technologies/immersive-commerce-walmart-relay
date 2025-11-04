# Client Secrets

A Client Id/Secret pair is used by the Auth Relay Server to request a bearer token used to access Walmart account linking and checkout APIs. For security purposes the Client Secret is returned to developers via the Unity Dashboard portal encrypted using the public key previously uploaded.

## Client Secret Decryption using openssl

Using command line tools available on most linux systems the public key can be easily decrypted.

```bash
# Decrypt the client secret using the private key
base64 -d -i /path/to/credentials_encrypted.key | openssl pkeyutl -decrypt -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256 -inkey /path/to/private_key.pem
```

## Client Secret Decryption using Python

A sample script has been provided, [decrypt_client_secret.py](decrypt_client_secret.py), to decrypt the client secret using the private key. The encrypted secret can either be provided as a filename on the command line or piped to the script.

### Examples
    
```bash
# Decrypt the client secret using the private key
python3 decrypt_client_secret.py /path/to/private_key.pem /path/to/encrypted_client_secret.txt

cat /path/to/encrypted_client_secret.txt | python3 decrypt_client_secret.py /path/to/private_key.pem
```
