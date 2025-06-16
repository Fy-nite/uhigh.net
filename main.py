import os
import glob

def count_lines_in_cs_files():
    total_lines = 0
    total_files = 0
    total_dirs = 0
    blacklist = ['.git', 'bin', 'obj', 'packages', 'node_modules'] # Directories to skip
    
    # Walk through directory tree recursively
    for root, dirs, files in os.walk('.'):
        # Remove blacklisted directories from dirs to prevent os.walk from traversing them
        dirs[:] = [d for d in dirs if d not in blacklist]
        
        total_dirs += len(dirs)
        
        # Find .cs files in current directory
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        # Count the number of lines in the file
                        line_count = sum(1 for _ in f)
                        print(f"{file_path}: {line_count} lines")
                        total_lines += line_count
                        total_files += 1
                except Exception as e:
                    print(f"Error reading {file_path}: {e}")
    
    print(f"\nTotal: {total_lines} lines in {total_files} .cs files")
    print(f"Directories scanned: {total_dirs}")

if __name__ == "__main__":
    count_lines_in_cs_files()