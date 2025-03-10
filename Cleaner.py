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