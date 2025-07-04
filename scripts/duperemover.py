from collections import Counter
import sys

def remove_duplicates_with_count(input_path, output_path):
    with open(input_path, 'r') as f:
        lines = [line.rstrip('\n') for line in f]

    counts = Counter(lines)
    unique_lines = []
    seen = set()

    for line in lines:
        if line not in seen:
            unique_lines.append(f"{line} : count {counts[line]}")
            seen.add(line)

    with open(output_path, 'w') as f:
        for line in unique_lines:
            f.write(line + '\n')

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python duperemover.py <input_file> <output_file>")
    else:
        remove_duplicates_with_count(sys.argv[1], sys.argv[2])