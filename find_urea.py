import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("--- Searching for UREA in dbo.ChemReagParam ---")
    cursor.execute("SELECT * FROM dbo.ChemReagParam WHERE [ChemName] LIKE '%UREA%'") # Assuming ChemName is the 4th column name based on output
    # Actually I should check column names first to be safe or just use * and python filtering if column name is unknown
    # But from prev output: (1, 1, 1, 'ALAT', ...) -> 4th col is name.
    
    # Let's get column names to be proper
    cursor.execute("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChemReagParam'")
    cols = [r.COLUMN_NAME for r in cursor.fetchall()]
    print(f"Columns: {cols}")
    
    # Assuming the text column is ChemName or similar
    name_col = next((c for c in cols if 'Name' in c or 'Chem' in c), None) # Heuristic
    
    if name_col:
        cursor.execute(f"SELECT * FROM dbo.ChemReagParam WHERE {name_col} LIKE '%UREA%'")
        rows = cursor.fetchall()
        for row in rows:
            print(row)
    else:
        print("Could not identify name column.")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
