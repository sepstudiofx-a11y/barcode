import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    barcode = "01021251130301304777"
    print(f"Checking for barcode {barcode} in dbo.Reagent...")
    
    cursor.execute(f"SELECT * FROM dbo.Reagent WHERE BarCode = '{barcode}'")
    rows = cursor.fetchall()
    
    if rows:
        print("MATCH FOUND in Database:")
        for row in rows:
            print(row)
    else:
        print("No match found in database.")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
