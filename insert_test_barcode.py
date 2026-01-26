import pyodbc
import datetime

server = '(localdb)\\MSSQLLocalDB'
database = 'BA80'
driver = '{ODBC Driver 17 for SQL Server}'

def calculate_weighted_sum(s):
    sum_val = 0
    length = len(s)
    # Logic from BarcodeService.cs
    # int digit = s[len - 1 - j] - '0';
    # int weight = (j % 2 == 0) ? 3 : 1;
    for j in range(length):
        digit = int(s[length - 1 - j])
        weight = 3 if (j % 2 == 0) else 1
        sum_val += digit * weight
    return sum_val

def generate_barcode(lot, serial, exp_date):
    # Fixed for UREA IIGEN based on observation/code
    ic = "004"
    bc = "2" # From existing row
    rc = "1" # From existing row
    dt = exp_date.strftime("%y%m%d")
    
    lot_str = str(lot).zfill(3)[-3:]
    serial_str = str(serial).zfill(4)[-4:]
    s4 = serial_str
    
    # Fallback logic from BarcodeService.cs for unknown anchor
    # pLotPart = ((int.Parse(s4[^1..]) * 3 + 5) % 10).ToString() + new string((i.LotNumber ?? "0").Where(char.IsDigit).ToArray()).PadLeft(3, '0');
    last_digit_s = int(s4[-1])
    p_digit = (last_digit_s * 3 + 5) % 10
    pLotPart = f"{p_digit}{lot_str}"
    
    currentPrefix = ic + bc + rc
    cFinal = currentPrefix + dt + pLotPart + s4
    
    weight = calculate_weighted_sum(cFinal)
    cs = (10 - weight % 10) % 10
    
    full_barcode = cFinal + str(cs)
    return full_barcode

try:
    conn_str = f'DRIVER={driver};SERVER={server};DATABASE={database};Trusted_Connection=yes;'
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    
    # Get template row using ReagentID 48 (UREA IIGEN)
    chem_reag_uid = 48 # From previous step
    cursor.execute(f"SELECT * FROM dbo.Reagent WHERE ReagentID = {chem_reag_uid}")
    template_row = cursor.fetchone()
    
    if not template_row:
        print("Template row not found!")
        exit()
        
    print(f"Template Row Found: {template_row}")
    
    # Define new values
    new_lot = "999"
    new_serial = "1001"
    new_exp = datetime.datetime(2026, 12, 31, 23, 59, 59)
    new_stable_hour = 0
    # Map from template
    # We need to construct the INSERT statement dynamically or manually
    # Columns from map_columns.py:
    # 0: UID (Identity? handle automatically), 1: BarCode, 2: TrayNum, 3: PosOnTray, 
    # 4: ExpirationDate, 5: ReagOpenDate, 6: OnInstrumentStableHour, 7: LotNum, 8: SN, 
    # 9: ReagStatus, 10: ReagVolume, 11: ReagBottleSpec, 12: ChemUID, 13: ReagType, 
    # 14: AvailTestNum, 15: ReagGroupUID, 16: ErrorCode, 17: MaxTestNum, 18: ModuleID, 
    # 19: RelatedChemUID, 20: SystemID, 21: ReagentID, 22: RecentUsed, 23: ReagTrayNo, 
    # 24: AlarmLimit, 25: MunualLoadFlag, 26: ContentRegion
    
    new_barcode = generate_barcode(new_lot, new_serial, new_exp)
    print(f"Generated Barcode: {new_barcode}")
    
    # Construct values from template, modifying key fields
    # Skip UID (col 0)
    # Barcode (1) -> new_barcode
    # ExpirationDate (4) -> new_exp
    # LotNum (7) -> new_lot
    # SN (8) -> new_serial
    # Others -> copy
    
    # Helper to format value for SQL
    def fmt(v):
        if isinstance(v, str): return f"'{v}'"
        if isinstance(v, datetime.datetime): return f"'{v.strftime('%Y-%m-%d %H:%M:%S')}'"
        if v is None: return "NULL"
        return str(v)
    
    # Columns to insert (INCLUDING UID this time)
    cols = [desc[0] for desc in cursor.description] # Include UID
    
    # Get Max UID
    cursor.execute("SELECT MAX(UID) FROM dbo.Reagent")
    max_uid = cursor.fetchone()[0]
    next_uid = int(max_uid) + 1
    print(f"Next UID: {next_uid}")
    
    # Build values list
    vals = []
    for col in cols:
        if col == 'UID': vals.append(str(next_uid))
        elif col == 'BarCode': vals.append(fmt(new_barcode))
        elif col == 'ExpirationDate': vals.append(fmt(new_exp))
        elif col == 'ReagOpenDate': vals.append(fmt(datetime.datetime.now())) # Use updated time
        elif col == 'LotNum': vals.append(fmt(new_lot.zfill(3)))
        elif col == 'SN': vals.append(fmt(new_serial.zfill(4)))
        else:
            # Find index in template row. 
            # Since we included UID in cols, index matches directly
            idx_in_template = cols.index(col) 
            vals.append(fmt(template_row[idx_in_template]))
            
    sql = f"INSERT INTO dbo.Reagent ({', '.join(cols)}) VALUES ({', '.join(vals)})"
    
    print("\nExecuting INSERT...")
    cursor.execute(sql)
    conn.commit()
    print("Insert successful!")
    print(f"** PLEASE TEST BARCODE: {new_barcode} **")
    
    conn.close()

except Exception as e:
    print(f"Error: {e}")
