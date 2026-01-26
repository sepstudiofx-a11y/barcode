import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("--- Getting ChemUID for UREA IIGEN ---")
    cursor.execute("SELECT * FROM dbo.ChemReagParam WHERE ChemName = 'UREA IIGEN'")
    urea_row = cursor.fetchone()
    if urea_row:
        print(f"UREA Row: {urea_row}")
        chem_uid = urea_row.ChemUID 
        print(f"ChemUID: {chem_uid}")
        
        print(f"\n--- Checking dbo.Reagent for ReagentID = {chem_uid} ---")
        cursor.execute(f"SELECT TOP 5 * FROM dbo.Reagent WHERE ReagentID = {chem_uid}")
        reagent_rows = cursor.fetchall()
        if reagent_rows:
            for r in reagent_rows:
                print(r)
        else:
            print("No existing Reagent rows found for this ID.")
            
    else:
        print("UREA IIGEN not found.")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
