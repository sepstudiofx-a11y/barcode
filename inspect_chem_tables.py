import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    # Just list everything in dictionary or similar
    print("--- First 10 rows of dbo.SITest ---")
    cursor.execute("SELECT TOP 10 * FROM dbo.SITest")
    for row in cursor.fetchall(): print(row)

    print("\n--- First 10 rows of dbo.ChemReagParam ---")
    cursor.execute("SELECT TOP 10 * FROM dbo.ChemReagParam")
    for row in cursor.fetchall(): print(row)

    print("\n--- First 10 rows of dbo.SITestResult (might show test names) ---")
    # Actually just assume SITest has names
    
    conn.close()

except Exception as e:
    print(f"Error: {e}")
