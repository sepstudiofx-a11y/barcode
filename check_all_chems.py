import pyodbc

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

# List of all chemicals from user's data
chemicals = [
    "ALAT", "CREA ENZ", "MG", "UREA IIGEN", "AMYL", "ASAT", "CA ARS", 
    "CHOL", "GGT", "GLUC", "GTT", "HbA1c D", "HDL D", "LDL D", 
    "PHOSPHORUS", "RF", "TRIGLYCERIDES", "TOTAL IgE", "UA II GEN",
    "CK", "CRP ULTRA", "CALCIUM ARSENAZO", "BILIRUBIN DIRECT", 
    "AMYLASE", "ALP", "ALBUMIN"
]

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    print("Checking which chemicals exist in database:\n")
    found = []
    not_found = []
    
    for chem in chemicals:
        cursor.execute("SELECT ChemUID, ChemName FROM dbo.ChemReagParam WHERE ChemName LIKE ?", f"%{chem}%")
        row = cursor.fetchone()
        if row:
            found.append(f"{chem} -> {row.ChemName} (ID: {row.ChemUID})")
        else:
            not_found.append(chem)
    
    print("FOUND IN DATABASE:")
    for f in found:
        print(f"  ✓ {f}")
    
    print(f"\nNOT FOUND ({len(not_found)}):")
    for nf in not_found:
        print(f"  ✗ {nf}")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
