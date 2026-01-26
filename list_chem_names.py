import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("--- Distinct ChemName in dbo.ChemReagParam ---")
    cursor.execute("SELECT DISTINCT ChemName FROM dbo.ChemReagParam")
    rows = cursor.fetchall()
    for row in rows:
        print(row.ChemName)

    conn.close()

except Exception as e:
    print(f"Error: {e}")
