import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    print(f"Connecting to {server}...")
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("Listing columns for dbo.Reagent:")
    cursor.execute("SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reagent'")
    cols = cursor.fetchall()
    for col in cols:
        print(col)

    print("\nSample data from dbo.Reagent (top 5):")
    cursor.execute("SELECT TOP 5 * FROM dbo.Reagent")
    rows = cursor.fetchall()
    for row in rows:
        print(row)
        
    conn.close()

except Exception as e:
    print(f"Error: {e}")
