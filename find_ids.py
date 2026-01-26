import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    # Try to find UREA or IgE in likely tables
    likely_tables = ['dbo.SITest', 'dbo.CalcChem', 'dbo.ISEChem', 'dbo.PanelChem', 'dbo.CalibChem', 'dbo.Code_Chem'] # Code_Chem might not exist, checking likely names from previous list
    # Checking specific tables from the list we saw earlier
    tables_to_check = ['dbo.SITest', 'dbo.CalcChem', 'dbo.ISEChem', 'dbo.PanelChem', 'dbo.Dictionary', 'dbo.CalibChem']

    for table in tables_to_check:
        try:
            print(f"--- Checking {table} for 'UREA' or 'IgE' ---")
            # Get columns to query text
            cursor.execute(f"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.split('.')[-1]}'")
            columns = [row.COLUMN_NAME for row in cursor.fetchall()]
            
            # Construct a loose search query
            conditions = [f"[{col}] LIKE '%UREA%'" for col in columns] # Basic check
            if conditions:
                query = f"SELECT * FROM {table} WHERE " + " OR ".join(conditions)
                cursor.execute(query)
                rows = cursor.fetchall()
                if rows:
                    print(f"Found match in {table}:")
                    for row in rows:
                        print(row)
                else:
                    print("No match.")
        except Exception as e:
            print(f"Skipping {table}: {e}")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
