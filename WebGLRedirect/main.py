import functions_framework
from flask import render_template_string

INDEX_TEMPLATE = """
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
</head>
<body>
code: {{ code }}
nonce: {{ nonce }}
</body>
</html>
"""

@functions_framework.http
def hello_http(request):
    """HTTP Cloud Function.
    Args:
        request (flask.Request): The request object.
        <https://flask.palletsprojects.com/en/1.1.x/api/#incoming-request-data>
    Returns:
        The response text, or any set of values that can be turned into a
        Response object using `make_response`
        <https://flask.palletsprojects.com/en/1.1.x/api/#flask.make_response>.
    """
    request_json = request.get_json(silent=True)
    request_args = request.args

    if request_args and 'code' in request_args:
        code = request_args['code']
    else:
        code = 'error'

    if request_args and 'nonce' in request_args:
        nonce = request_args['nonce']
    else:
        nonce = 'error'

    return render_template_string(
        INDEX_TEMPLATE, code=code, nonce=nonce)

