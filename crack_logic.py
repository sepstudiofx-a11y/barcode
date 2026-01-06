
import sys
from itertools import product

# Samples: Full 20 chars
# We are interested in Index 10 (P) and Index 19 (Checksum)
# Format: 0123456789 P 12345678 Serial Checksum
# Actually, let's look at the structure again.
# S1: 0342124083 1 30518696 7 (Length 20? No wait)
# S1: 03421240831305186967 (Len 20)
# Index 0-9: 0342124083
# Index 10: 1 ?? Wait.
# User Table S1: 0342124083 1 30518696 7
# My previous analysis said P is Index 10.
# Let's count S1:
# 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9
# 0 3 4 2 1 2 4 0 8 3 1 3 0 5 1 8 6 9 6 7
# Index 10 is '1'.
# User table says: 0342124083 1 30518696 7
# Wait, S1 in table in prompt: 
# "0342124083 1 30518696 7"
# S1 in my code anchors: "0342124083130518696" (19 chars) + Checksum "7"?
# No, user prompt S1: "03421240831305186967" (20 chars).
# Digits:
# 0-9: 0342124083
# 10: 1
# 11-18: 30518696
# 19: 7

# S2: 0341224083 1 90518716 5
# Index 10: 1. (Same as S1 !!)
# WAIT. In my previous thought I said P changed.
# Let's re-read the table carefully.
# 1: 0342124083 1 3 0518696 7 (Total 20?)
# 0342124083 (10)
# 1 (1) -> Index 10
# 3 (1) -> Index 11
# 051 (3) -> Lot
# 8696 (4) -> Serial
# 7 (1) -> Checksum
# Total: 10+1+1+3+4+1 = 20.
# So Index 10 is '1'. Index 11 is '3'.
# S2: 0341224083 1 9 0518716 5
# Index 10 is '1'. Index 11 is '9'.
# So Index 11 is the one changing!
# Let's call Index 11 "Q".
# S7: 0102124093 0 9 0098931 1
# Index 10 is '0' (Wait, S7 starts 0102124093, 10th char is '9'?)
# Let's align S7 carefully.
# 0102124093 09 0098931 1
# 0-9: 0102124093
# 10: 0
# 11: 9
# 12-19: 00989311
# ok, let's dump the samples into code and parse them mechanically.

samples = [
    "03421240831305186967",
    "03412240831905187165",
    "03421241130605287237",
    "03412241130805287467",
    "03421250531105397211",
    "03412250531305397641",
    "01021240930900989311",
    "01021240930100989451",
    "01012240930100994395",
    "01021241130001005591",
    "01021241130101005561",
    "01012241130501010681",
    "01021251130301304777",
    "01021251130001304769",
    "01012251130701301175"
]

def analyze_positional_changes():
    # Convert to list of lists of ints
    data = [[int(c) for c in s] for s in samples]
    
    # Check which columns are constant or variable
    cols = range(20)
    for c in cols:
        col_vals = [row[c] for row in data]
        unique = set(col_vals)
        print(f"Col {c}: {unique} {'CONSTANT' if len(unique)==1 else 'VAR'}")
        
    print("-" * 20)
    
    # Focus on Col 10, 11, 19
    # Try to find linear relation for Col 11 (Q) and Col 19 (Check)
    # Target = Col 11. Inputs = Col 0..10 + 12..18?
    # It seems Col 11 changes with Serial.
    
    # Let's brute force formatted logic for Col 11.
    # It might be Check Digit 1.
    pass

def solve_linear(target_idx, input_indices, modulus=10):
    print(f"Solving for Target Index {target_idx} (Mod {modulus})...")
    data = [[int(c) for c in s] for s in samples]
    
    Y = [row[target_idx] for row in data]
    X = [[row[i] for i in input_indices] for row in data]
    
    # Brute force weights
    L = len(input_indices)
    
    # Heuristic: Weights likely [1, 3, 7, 9] (odd)
    # We'll try small weights range -5 to 5 or 1 to 9.
    
    # Attempt 1: Weights 1, 3, 7, 9
    from itertools import product
    
    for pat in product([1, 3, 7, 9], repeat=L):
        valid = True
        for i in range(len(samples)):
            s = sum(w * x for w, x in zip(pat, X[i]))
            if (s % modulus) != (Y[i] % modulus) and ((modulus - s%modulus)%modulus != Y[i]):
                 # Try both s%10 == t and (10-s)%10 == t
                 valid = False
                 break
        
        # Check standard Luhn-like: Sum + Target = 0 mod 10
        if not valid:
            valid_luhn = True
            for i in range(len(samples)):
                s = sum(w * x for w, x in zip(pat, X[i]))
                if (s + Y[i]) % modulus != 0:
                    valid_luhn = False
                    break
            if valid_luhn:
                print(f"!!! FOUND LUHN-LIKE for Idx {target_idx}: Weights {pat}, Mod {modulus}")
                return

        if valid:
             # Check which rule
            rule1 = all((sum(w*x for w,x in zip(pat, X[i])) % modulus) == Y[i] for i in range(len(samples)))
            if rule1:
                print(f"!!! FOUND DIRECT SUM for Idx {target_idx}: Weights {pat}, Mod {modulus}")
                return
    
    print(f"No simple linear pattern for Idx {target_idx}.")

if __name__ == "__main__":
    analyze_positional_changes()
    
    # Try to solve for Digit 11 (Index 11, 0-based? No 10-based?)
    # 0..9 (10 digits)
    # 10 (1 digit)
    # 11 (1 digit)
    # Let's solve for Index 11 using indices 15-18 (Serial)
    solve_linear(11, [15, 16, 17, 18], 10)
    
    # Solve for Index 19 (Checksum) using all others
    # indices 0..18
    solve_linear(19, list(range(19)), 10)
