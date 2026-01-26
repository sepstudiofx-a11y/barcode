import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    # Get UREA ChemUID again simply
    cursor.execute("SELECT ChemUID FROM dbo.ChemReagParam WHERE ChemName = 'UREA IIGEN'")
    uid = cursor.fetchone()[0]
    
    # Get column names
    cursor.execute("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reagent' ORDER BY ORDINAL_POSITION")
    columns = [row.COLUMN_NAME for row in cursor.fetchall()]
    
    # Get the row
    cursor.execute(f"SELECT TOP 1 * FROM dbo.Reagent WHERE ReagentID = {uid}")
    row = cursor.fetchone()
    
    if row:
        print(f"--- Mapping for ReagentID {uid} ---")
        for i, val in enumerate(row):
            print(f"{i}: {columns[i]} = {val}")
    else:
        print("Row not found.")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
