import functions_framework
import re
import time
from flask import render_template_string, Response

SECURE_TEMPLATE = """
<!DOCTYPE html>
<html lang="en-us">
<head>
    <script>
        window.opener.postMessage("{{ code }},{{ nonce }}", "*")
     
        try 
        {
                window.close();
        }
        catch (e) { console.log(e) }

        try 
        {
                self.close();
        }
        catch (e) { console.log(e) }       
    </script>
    <meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'self'">
</head>
<body>
code: {{ code }}
nonce: {{ nonce }}
</body>
</html>
"""

def validate_oauth_param(param, param_name):
    """
    Validate OAuth parameters against expected format to prevent XSS.
    
    Args:
        param (str): Parameter value to validate
        param_name (str): Name of parameter for specific validation rules
        
    Returns:
        str: Validated parameter or 'error' if invalid
    """
    if not param or not isinstance(param, str):
        return 'error'
    
    # Remove any null bytes that could cause issues
    param = param.replace('\x00', '')
    
    if param_name == 'code':
        # OAuth authorization codes are typically 32 hexadecimal characters
        if re.match(r'^[A-Fa-f0-9]{32}$', param):
            return param
        else:
            return 'error'
            
    elif param_name == 'nonce':
        # Nonces can be UUIDs (with or without hyphens) or similar secure random strings
        if re.match(r'^[A-Fa-f0-9\-]{32,36}$', param):
            return param
        else:
            return 'error'
    
    # For any other parameters, reject to be safe
    return 'error'

@functions_framework.http
def hello_http(request):
    """
    Secure HTTP Cloud Function for OAuth callback handling.
    
    This function receives OAuth callback parameters from Walmart ICS
    and securely passes them to the WebGL application via postMessage.
    
    Security measures:
    - Input validation with regex patterns to prevent XSS
    - Parameter sanitization before template rendering
    - Content Security Policy headers
    - Error handling for invalid parameters
         
    Args:
        request (flask.Request): The request object containing OAuth parameters
        
    Returns:
        Response: HTML page with JavaScript to pass data to parent window
    """
    request_args = request.args
    
    # Extract and validate OAuth parameters
    raw_code = request_args.get('code', '')
    raw_nonce = request_args.get('nonce', '')
    
    # Validate parameters against expected OAuth formats
    validated_code = validate_oauth_param(raw_code, 'code')
    validated_nonce = validate_oauth_param(raw_nonce, 'nonce')
    
    # Render template with validated parameters
    html_content = render_template_string(
        SECURE_TEMPLATE,
        code=validated_code,
        nonce=validated_nonce
    )
    
    # Create response with security headers
    response = Response(html_content)
    
    # Add Content Security Policy header for additional XSS protection
    response.headers['Content-Security-Policy'] = (
        "default-src 'self'; "
        "script-src 'self' 'unsafe-inline'; "  # unsafe-inline needed for postMessage script
        "object-src 'none'; "
        "base-uri 'self'; "
        "frame-ancestors 'none'"
    )
    
    # Prevent caching of OAuth callback responses
    response.headers['Cache-Control'] = 'no-cache, no-store, must-revalidate'
    response.headers['Pragma'] = 'no-cache'
    response.headers['Expires'] = '0'
    
    # Add security headers
    response.headers['X-Content-Type-Options'] = 'nosniff'
    response.headers['X-Frame-Options'] = 'DENY'
    
    return response

