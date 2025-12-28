import json

chem_map = {
    "001": "GLUCOSE", "002": "CHOLESTEROL", "003": "TRIGLYCERIDES", "004": "ALBUMIN",
    "005": "TOTAL PROTEIN", "006": "BIL TOTAL", "007": "BILIRUBIN DIRECT", "009": "UA II GEN",
    "010": "UREA II GEN", "012": "MAGNESIUM", "013": "PHOSPHORUS", "015": "ALAT",
    "016": "ASAT", "017": "AMYLASE", "018": "ALP", "019": "CK", "020": "LDH",
    "022": "GGT", "024": "GTT", "025": "HDL DIRECT", "026": "LDL DIRECT",
    "027": "CRP ULTRA", "031": "RF", "074": "TOTAL IgE", "059": "CALCIUM ARSENAZO",
    "061": "HbA1c DIRECT", "071": "CREA ENZ"
}

def parse_expiry(f):
    if len(f) < 11: return "01/01/2025"
    yymmdd = f[5:11]
    return f"{yymmdd[2:4]}/{yymmdd[4:6]}/20{yymmdd[0:2]}"

def extract_lot(f):
    # Logic: 1 checksum digit (last), 4 serial digits (before checksum)
    # Lot is usually the 3 digits before the serial.
    total = len(f)
    if total < 10: return "000"
    lot_end = total - 5
    lot_start = lot_end - 3
    return f[lot_start:lot_end]

def extract_bottle(f):
    if len(f) > 3:
        b = f[3]
        if b == '1': return "20 ml"
        if b == '2': return "40 ml"
        if b == '3': return "60 ml"
    return "20 ml"

def extract_serial(f):
    # Extract the actual 4 digits used in the barcode string
    total = len(f)
    if total < 5: return "0000"
    return f[total-5:total-1]

try:
    with open('wwwroot/data/barcode_anchors.json', 'r') as f:
        data = json.load(f)
        
    with open('new_test_cases.txt', 'w') as out:
        out.write("        private List<TestCase> GetTestCases()\n")
        out.write("        {\n")
        out.write("            return new List<TestCase>\n")
        out.write("            {\n")
        
        count = 0
        for i, item in enumerate(data):
            if count >= 250: break
            
            ic = item.get('ic', '')
            rt = item.get('rt', 'R1')
            f_code = item.get('f', '')
            
            if not f_code: continue
            
            name = chem_map.get(ic, f"CHEM_{ic}")
            exp = parse_expiry(f_code)
            lot = extract_lot(f_code)
            bottle = extract_bottle(f_code)
            # Use the serial number as found in the barcode string itself to ensure we are testing the generation logic accurately
            serial = extract_serial(f_code)
            
            out.write(f'                new({i+1}, "{name}", "{ic}", "{bottle}", "{rt}", "{lot}", "{serial}", "{exp}", "{f_code}"),\n')
            count += 1
            
        out.write("            };\n")
        out.write("        }\n")
        
    print("SUCCESS")
except Exception as e:
    print(f"Error: {e}")
