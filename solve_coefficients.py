
import itertools

# Golden Samples
samples_raw = [
    # IgE (Item 034, Rgt 1/2 handled as same group usually? Or separte? Let's assume separate maybe?)
    # Format: (Item, Rgt, FullBarcode)
    ("034", "R1", "03421240831305186967"),
    ("034", "R2", "03412240831905187165"),
    ("034", "R1", "03421241130605287237"),
    ("034", "R2", "03412241130805287467"),
    ("034", "R1", "03421250531105397211"),
    ("034", "R2", "03412250531305397641"),
    
    # UREA (Item 010)
    ("010", "R1", "01021240930900989311"), # Lot 009
    ("010", "R1", "01021240930100989451"), # Lot 009
    ("010", "R2", "01012240930100994395"), # Lot 009
    ("010", "R1", "01021241130001005591"), # Lot 010
    ("010", "R1", "01021241130101005561"), # Lot 010
    ("010", "R2", "01012241130501010681"), # Lot 010
    ("010", "R1", "01021251130301304777"), # Lot 013
    ("010", "R1", "01021251130001304769"), # Lot 013
    ("010", "R2", "01012251130701301175")  # Lot 013
]

def parse(s):
    # P is usually derived from SN last digit logic in the service: 
    # pLotPart = ((int.Parse(s4[^1..]) * 3 + 5) % 10)
    # But wait, the service calculates P *if* sample not found.
    # If sample IS found, P is read from sample: sample.Full[11]
    
    # Let's verify if the P logic matches: ((last_digit_sn * 3 + 5) % 10)
    # IgE 1: SN 8696. Last=6. (6*3+5)%10 = (18+5)%10 = 3. 
    # Barcode: ...051 8696 -> Barcode[11] (0-index) -> 0342124083 1 3... -> Digit 11 is '3'. MATCH.
    
    # UREA 1: SN 8931. Last=1. (1*3+5)%10 = 8.
    # Barcode: ...009 8931 -> ...0930 9 009... -> Digit 11 is '9'. NO MATCH.
    # Service Logic for P:
    # "int pSampleNum = (sample.Full.Length >= 12) ? sample.Full[11] - '0' : 0;"
    # It reads P from the SAMPLE.
    
    # We need to find k, m, l such that:
    # CS_new = (CS_anchor + W_anchor_delta + k * dP + m * dSTens + l * dLot) % 10
    
    # But wait, W_anchor_delta is CalculateWeightedSum(sample.Full[..^1]).
    # The service logic is complex.
    
    # SIMPLIFIED GOAL:
    # Find a checksum formula that works for ALL samples in a group.
    # Formula: (WeightedSum(First19) + Adjustment) % 10 == LastDigit
    # Adjustment might be constant, or depend on Lot/Serial.
    return

# Let's brute force the service's own coefficients k, m, l for the defined samples.
# We create equations for every pair in the group.

def solve_coefficients(group_samples, group_name):
    # data: list of (p, s_tens, lot, cs, w_sum_19)
    # p is digit 11
    # s_tens is SN / 10
    # lot is digit 12-14
    # cs is digit 19
    # w_sum_19 is weighted sum of first 19 digits (standard 3,1,3,1...)
    
    def calc_wsum(s):
        sum_v = 0
        for i, c in enumerate(s):
            w = 3 if i % 2 == 0 else 1
            sum_v += int(c) * w
        return sum_v

    pts = []
    for _, _, code in group_samples:
        p = int(code[11])
        lot = int(code[12:15])
        sn = int(code[15:19])
        s_tens = sn // 10
        cs = int(code[19])
        
        # Determine "Base Checksum" from Weighted Sum
        # Service logic: int currentWSum = CalculateWeightedSum(cFinal);
        # int cs = (targetCal - currentWSum) % 10;
        # => targetCal = (cs + currentWSum) % 10
        
        # And targetCal = (anchorCal + k*dP + m*dS + l*dLot)
        # For a single sample, we can say:
        # targetCal_i = (C + k*p_i + m*s_tens_i + l*lot_i) % 10
        
        ws = calc_wsum(code[:19])
        target_cal = (cs + ws) % 10
        
        pts.append({'p': p, 's': s_tens, 'l': lot, 't': target_cal})

    # Brute force k, m, l, C (Base constant) in range [0..9]
    print(f"Solving {group_name} with {len(pts)} points...")
    
    solutions = []
    for k in range(10):
        for m in range(10):
            for l in range(10):
                for C in range(10):
                    valid = True
                    for pt in pts:
                        # Predicted Target Cal
                        pred = (C + k*pt['p'] + m*pt['s'] + l*pt['l']) % 10
                        if pred != pt['t']:
                            valid = False
                            break
                    if valid:
                        solutions.append((k, m, l, C))
    
    if solutions:
        print(f"FOUND {len(solutions)} Solutions for {group_name}!")
        for s in solutions:
            print(f"  k={s[0]}, m={s[1]}, l={s[2]}, C={s[3]}")
    else:
        print(f"No linear solution found for {group_name}.")

# Group samples
ige = [s for s in samples_raw if s[0] == "034"]
urea = [s for s in samples_raw if s[0] == "010"]

solve_coefficients(ige, "IgE")
solve_coefficients(urea, "UREA")
