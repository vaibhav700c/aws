from mangum import Mangum
from fastapi_server import app

# Wrap FastAPI app with Mangum for AWS Lambda
handler = Mangum(app)