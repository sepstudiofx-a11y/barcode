
import sys
from itertools import product

# Samples (First 19 digits -> 20th digit)
samples = [
    ("0342124083130518696", 7),
    ("0341224083190518716", 5),
    ("0342124113060528723", 7),
    ("0341224113080528746", 7),
    ("0342125053110539721", 1),
    ("0341225053130539764", 1),
    ("0102124093090098931", 1),
    ("0102124093010098945", 1),
    ("0101224093010099439", 5),
    ("0102124113000100559", 1),
    ("0102124113010100556", 1),
    ("0101224113050101068", 1),
    ("0102125113030130477", 7),
    ("0102125113000130476", 9),
    ("0101225113070130117", 5)
]

def solve_weights_pattern(modulus=10):
    print(f"Solving for Modulus {modulus}...")
    
    # Target values
    Y = []
    X = []
    
    for inputs, output in samples:
        digits = [int(c) for c in inputs]
        X.append(digits)
        
        # Assume Sum + Output = 0 (mod M) => Sum = -Output
        target = (modulus - output) % modulus
        Y.append(target)

    # Brute Force Patterns
    # Try pattern length L
    for L in range(1, 8): # Try lengths up to 7
        print(f"  Testing Pattern Length {L}...")
        
        # Brute force weights in range [1, 3, 7, 9] (common) + [2,4,5,6,8]
        # Just try 1..9
        for pat in product(range(1, 10), repeat=L):
            weights = (pat * 20)[:19]
            
            valid = True
            for i in range(len(samples)):
                # Manual dot product
                row_sum = sum(w * d for w, d in zip(weights, X[i]))
                if row_sum % modulus != Y[i]:
                    valid = False
                    break
            
            if valid:
                print(f"  !!! FOUND PATTERN LENGTH {L}: {pat}")
                print(f"  modulus: {modulus}")
                return weights

    print(f"  No simple pattern found for Mod {modulus}.")

if __name__ == "__main__":
    solve_weights_pattern(10)
    print("-" * 20)
    solve_weights_pattern(11)
