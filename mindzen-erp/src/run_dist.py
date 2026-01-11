
import os
import sys
import uvicorn
import multiprocessing

# Add the current directory to sys.path to ensure modules can be found
current_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, current_dir)

# Import the app
# Note: Adjust the import if mindzen_erp is a package in the same dir
from mindzen_erp.web import app

def main():
    # PyInstaller multiprocessing fix for Windows
    multiprocessing.freeze_support()
    
    # Run uvicorn Programmatically
    # passing 'app' object directly, NOT string, to avoid import issues in frozen mode
    # workers=1 is standard for desktop app usage
    uvicorn.run(app, host="0.0.0.0", port=8000, log_level="info", reload=False)

if __name__ == "__main__":
    main()
