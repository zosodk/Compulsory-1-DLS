import os
import re

def clean_mail(file_path, output_path):
    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()

        # Improved regex for header removal
        header_regex = r"^(Message-ID:|Mime-Version:|Content-Type:|Content-Transfer-Encoding:|X-.*?:|From:|To:|Cc:|Bcc:|Subject:|Date:|Received:|Forwarded by|[-]+ Forwarded by).*?\n"
        cleaned_content = re.sub(header_regex, "", content, flags=re.MULTILINE | re.IGNORECASE)

        # Remove empty lines
        cleaned_content = re.sub(r"^\s*\n", "", cleaned_content, flags=re.MULTILINE)

        with open(output_path, 'w', encoding='utf-8') as outfile:
            outfile.write(cleaned_content)
        return True
    except Exception as e:
        print(f"Error cleaning {file_path}: {e}")
        return False

def process_files(input_folder, output_folder):
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    for root, dirs, files in os.walk(input_folder):
        print(f"Checking directory: {root}")
        for filename in files:
            print(f"  Found file: {filename}")

            match = re.match(r".*_$", filename)  # Corrected regex
            print(f"    Regex match: {match}")

            if match:
                input_file = os.path.join(root, filename)
                relative_root = os.path.relpath(root, input_folder)
                output_dir = os.path.join(output_folder, relative_root)

                if not os.path.exists(output_dir):
                    os.makedirs(output_dir)

                output_file = os.path.join(output_dir, filename + ".txt")

                if clean_mail(input_file, output_file):
                    print(f"    Cleaned {input_file}")

if __name__ == "__main__":
    print(f"Current working directory: {os.getcwd()}")
    input_folder = 'litmus_maildir'
    output_folder = 'litmus_cleaned_mails'
    process_files(input_folder, output_folder)