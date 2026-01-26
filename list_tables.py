import pyodbc

# Try connecting to the discovered LocalDB instance
server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    print(f"Connecting to {server}...")
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print(f"Connected to {database}. Listing tables...")
    cursor.execute("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
    
    tables = cursor.fetchall()
    for row in tables:
        print(f"{row.TABLE_SCHEMA}.{row.TABLE_NAME}")
        
    conn.close()
    print("Done.")

except Exception as e:
    print(f"Error connecting to {server}: {e}")
    # Fallback to master to list databases
    try:
        print("Listing databases on server...")
        conn_str = f'DRIVER={driver};SERVER={server};DATABASE=master;Trusted_Connection=yes;'
        conn = pyodbc.connect(conn_str)
        cursor = conn.cursor()
        cursor.execute("SELECT name FROM sys.databases")
        dbs = cursor.fetchall()
        print("Databases found:")
        for db in dbs:
            print(db.name)
        conn.close()
    except Exception as e2:
        print(f"Error connecting to master: {e2}")
