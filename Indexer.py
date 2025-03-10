import os
from elasticsearch import Elasticsearch
from datetime import datetime

es = Elasticsearch([{'host': 'elasticsearch', 'port': 9200, 'scheme': 'http'}]) # Add scheme='http'

def index_file(file_name, file_path, cleaned_content):
    try:
        doc = {
            'file_name': file_name,
            'file_path': file_path,
            'cleaned_content': cleaned_content,
            'indexed_at': datetime.now()
        }
        es.index(index='mail_index', document=doc)
        print(f"Indexed {file_path}")
    except Exception as e:
        print(f"Error indexing {file_path}: {e}")

def process_cleaned_files(cleaned_folder):
    for root, dirs, files in os.walk(cleaned_folder):
        for filename in files:
            if filename.endswith(".txt"):
                file_path = os.path.join(root, filename)
                relative_path = os.path.relpath(file_path, cleaned_folder)
                with open(file_path, 'r', encoding='utf-8') as f:
                    cleaned_content = f.read()
                index_file(filename, relative_path, cleaned_content)

if __name__ == "__main__":
    cleaned_folder = "litmus_cleaned_mails"
    process_cleaned_files(cleaned_folder)