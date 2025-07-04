from collections import Counter
import sys
import re

def extract_type(line):
    # Regex to match: The type or namespace name 'Emit'
    match = re.search(r"The type or namespace name '([^']+)'", line)
    if match:
        return match.group(1)
    return None

def remove_duplicates_with_count(input_path, output_path):
    with open(input_path, 'r') as f:
        lines = [line.rstrip('\n') for line in f]

    # Extract types/namespaces from error lines
    types = [extract_type(line) for line in lines]
    types = [t for t in types if t]  # Remove None values

    counts = Counter(types)
    unique_types = []
    seen = set()

    for t in types:
        if t not in seen:
            unique_types.append((t, counts[t]))
            seen.add(t)

    with open(output_path, 'w') as f:
        f.write("TypeOrNamespace\tCount\n")
        for t, count in unique_types:
            f.write(f"{t}\t{count}\n")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python duperemover.py <input_file> <output_file>")
    else:
        remove_duplicates_with_count(sys.argv[1], sys.argv[2])