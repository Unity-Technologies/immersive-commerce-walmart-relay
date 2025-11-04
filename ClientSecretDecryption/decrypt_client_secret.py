import sys
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.serialization import load_pem_private_key
from cryptography.hazmat.backends import default_backend
import base64

def read_secret(secret_file=None):
    """
    Read a secret either from stdin or from a file

    :param secret_file: Path to the file containing the secret, or None to read from stdin
    :return: The secret as a string
    """

    if secret_file is None:
        return sys.stdin.read()
    else:
        with open(secret_file, "r") as secret_input:
            return secret_input.read()


def decrypt_with_rsa_private_key(encrypted_data, private_key_pem_file, password=None):
    """
    Decrypt data using an RSA private key

    :param encrypted_data: Base64 decoded client secret from Walmart
    :param private_key_pem_file: Your private key file
    :param password: Optional password for the private key
    :return: The decrypted client secret
    """

    with open(private_key_pem_file, "rb") as private_key_pem:
        # Load the private key
        private_key = load_pem_private_key(private_key_pem.read(), password=password, backend=default_backend())
    
        # Decrypt the data
        decrypted_data = private_key.decrypt(
            encrypted_data,
            padding.OAEP(
                mgf=padding.MGF1(algorithm=hashes.SHA256()),
                algorithm=hashes.SHA256(),
                label=None
            )
        )
        return decrypted_data


if __name__ == "__main__":
    """
    Usage: python decrypt_client_secret.py <private_key_file> [<encrypted_client_secret_file>]
    
    Decrypts the encrypted client secret using the provided private key. If the client secret file is not provided, it reads from stdin.
    
    Example:
    python decrypt_client_secret.py /path/to/private_key.pem /path/to/encrypted_client_secret.txt
    or
    cat /path/to/encrypted_client_secret.txt | python decrypt_client_secret.py /path/to/private_key.pem
    """

    if len(sys.argv) < 3:
        client_secret = read_secret()
    else:
        client_secret = read_secret(sys.argv[2])

    try:
        decoded_client_secret = base64.b64decode(client_secret)
    except (binascii.Error, ValueError) as e:
        print(f"Error: Failed to decode base64 client secret. {e}", file=sys.stderr)
        sys.exit(1)

    try:
        results = decrypt_with_rsa_private_key(decoded_client_secret, sys.argv[1])
    except Exception as e:
        print(f"Error: Failed to decrypt client secret. {e}", file=sys.stderr)
        sys.exit(1)

    print(results.decode('utf-8'))
