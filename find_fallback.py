import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("--- Searching for Wash/Clean/Diluent in dbo.ChemReagParam ---")
    cursor.execute("SELECT * FROM dbo.ChemReagParam WHERE ChemName LIKE '%Wash%' OR ChemName LIKE '%Clean%' OR ChemName LIKE '%Dil%'")
    rows = cursor.fetchall()
    if rows:
        for row in rows:
            print(row)
    else:
        print("No Wash/Clean chemicals found.")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
