
import itertools

samples_raw = [
    ("03421240831305186967"), ("03412240831905187165"), ("03421241130605287237"),
    ("03412241130805287467"), ("03421250531105397211"), ("03412250531305397641"),
    ("01021240930900989311"), ("01021240930100989451"), ("01012240930100994395"),
    ("01021241130001005591"), ("01021241130101005561"), ("01012241130501010681"),
    ("01021251130301304777"), ("01021251130001304769"), ("01012251130701301175")
]

data = []
for s in samples_raw:
    digits = [int(c) for c in s[:19]]
    checksum = int(s[19])
    data.append((digits, checksum))

print(f"Solving for {len(data)} samples...")

def check(weights, modulus=10, remainder_mode=True):
    # remainder_mode: True = (10 - sum%10)%10, False = sum%10
    score = 0
    for digits, expected in data:
        s = sum(w * d for w, d in zip(weights, digits))
        if remainder_mode:
            calc = (modulus - (s % modulus)) % modulus
        else:
            calc = s % modulus
        if calc == expected:
            score += 1
    return score

# Try cycles of length 2 to 6
for L in range(2, 7):
    print(f"Testing cycles of length {L}...")
    # Brute force all combinations of digits 1-9 for length L
    for pat in itertools.product(range(1, 10), repeat=L):
        full_weights = (pat * 20)[:19]
        
        # Test Mod 10 Remainder (Luhn style)
        if check(full_weights, 10, True) == 15:
            print(f"!!! FOUND MATCH (Mod 10 Remainder) !!! Pattern: {pat}")
            exit()
            
        # Test Mod 10 Standard
        if check(full_weights, 10, False) == 15:
            print(f"!!! FOUND MATCH (Mod 10 Direct) !!! Pattern: {pat}")
            exit()
            
        # Test Mod 11
        if check(full_weights, 11, True) == 15:
             print(f"!!! FOUND MATCH (Mod 11) !!! Pattern: {pat}")
             exit()

print("No Repeating Pattern Found.")

# If no simple pattern, try pure random solver for first 19 weights? 
# Maybe too large space 9^19.
# But we can try to solve for "IgE" and "UREA" separately.

print("\nTrying Separate Solvers:")

ige_data = data[:6]
urea_data = data[6:]

def solve_sub(subdata, label):
    for L in range(2, 6):
        for pat in itertools.product(range(1, 10), repeat=L):
            full_weights = (pat * 20)[:19]
            if sum(1 for d, e in subdata if (10 - sum(w*x for w,x in zip(full_weights,d))%10)%10 == e) == len(subdata):
                print(f"{label} MATCH (Mod 10 Remainder): {pat}")
                return

solve_sub(ige_data, "IgE")
solve_sub(urea_data, "UREA")
