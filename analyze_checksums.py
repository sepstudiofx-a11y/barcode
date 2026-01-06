
import math

# Golden Samples from User Request
samples = [
    # IgE
    ("IgE", "03421240831305186967"), # Lot 051, SN 8696
    ("IgE", "03412240831905187165"), # Lot 051, SN 8716
    ("IgE", "03421241130605287237"), # Lot 052, SN 8723
    ("IgE", "03412241130805287467"), # Lot 052, SN 8746
    ("IgE", "03421250531105397211"), # Lot 053, SN 9721
    ("IgE", "03412250531305397641"), # Lot 053, SN 9764
    
    # UREA
    ("UREA", "01021240930900989311"), # Lot 009, SN 8931
    ("UREA", "01021240930100989451"), # Lot 009, SN 8945
    ("UREA", "01012240930100994395"), # Lot 009, SN 9439
    ("UREA", "01021241130001005591"), # Lot 010, SN 0559
    ("UREA", "01021241130101005561"), # Lot 010, SN 0556
    ("UREA", "01012241130501010681"), # Lot 010, SN 1068
    ("UREA", "01021251130301304777"), # Lot 013, SN 0477
    ("UREA", "01021251130001304769"), # Lot 013, SN 0476
    ("UREA", "01012251130701301175")  # Lot 013, SN 0117
]

def calculate_checksum_algorithms(code_19):
    results = {}
    
    # Standard Mod 10 (Luhn)
    sum_luhn = 0
    reverse = code_19[::-1]
    for i, digit in enumerate(reverse):
        n = int(digit)
        if i % 2 == 0:
            n *= 2
            if n > 9: n -= 9
        sum_luhn += n
    results['Luhn'] = (10 - (sum_luhn % 10)) % 10
    
    # Weighted Mod 10 (3,1,3,1...) - Odd positions x3
    sum_w31 = 0
    for i, digit in enumerate(code_19):
        w = 3 if i % 2 == 0 else 1
        sum_w31 += int(digit) * w
    results['Mod10_31'] = (10 - (sum_w31 % 10)) % 10
    
    # Weighted Mod 10 (1,3,1,3...) - Even positions x3
    sum_w13 = 0
    for i, digit in enumerate(code_19):
        w = 1 if i % 2 == 0 else 3
        sum_w13 += int(digit) * w
    results['Mod10_13'] = (10 - (sum_w13 % 10)) % 10

    # Mod 11
    sum_mod11 = 0
    weights = [2,3,4,5,6,7,8,9]
    for i, digit in enumerate(code_19[::-1]):
        sum_mod11 += int(digit) * weights[i % 8]
    rem = sum_mod11 % 11
    if rem <= 1: results['Mod11'] = 0 
    else: results['Mod11'] = 11 - rem

    return results

print(f"{'Type':<6} {'Barcode (19)':<20} {'Act':<3} {'Luhn':<4} {'31':<4} {'13':<4} {'M11':<4}")
print("-" * 60)

for type_name, full_code in samples:
    code_19 = full_code[:19]
    actual_cs = int(full_code[19])
    calcs = calculate_checksum_algorithms(code_19)
    
    match_str = ""
    for k, v in calcs.items():
        if v == actual_cs:
            match_str = k
            break
            
    print(f"{type_name:<6} {code_19:<20} {actual_cs:<3} {calcs['Luhn']:<4} {calcs['Mod10_31']:<4} {calcs['Mod10_13']:<4} {calcs['Mod11']:<4} {match_str}")

print("\n--- Delta Analysis ---")
# Compare pairs to see how changes affect CS
for i in range(len(samples)):
    for j in range(i + 1, len(samples)):
        t1, c1 = samples[i]
        t2, c2 = samples[j]
        if t1 == t2: # Compare same chem types
            s1 = int(c1[15:19])
            s2 = int(c2[15:19])
            cs1 = int(c1[19])
            cs2 = int(c2[19])
            
            diff_sn = s2 - s1
            diff_cs = (cs2 - cs1)
            
            # Simple check: Does CS change by +/- 1 when SN changes by 1?
            # Or weighted?
            
            print(f"{t1} Pair: SN {s1}->{s2} (d={diff_sn}), CS {cs1}->{cs2} (d={diff_cs})")
