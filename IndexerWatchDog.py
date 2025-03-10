import os
import psycopg2  # PostgreSQL library
import re
from collections import defaultdict
import time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# Database connection details (replace with your actual values)
DB_HOST = "localhost"
DB_NAME = "your_database_name"
DB_USER = "your_database_user"
DB_PASS = "your_database_password"

def create_word_index(content):
    """
    Creates a simple word index by counting word occurrences.

    Args:
        content: The text content to index.

    Returns:
        A string representation of the word index (e.g., "word1:3,word2:5").
    """
    words = re.findall(r'\b\w+\b', content.lower())  # Extract words
    word_counts = defaultdict(int)
    for word in words:
        word_counts[word] += 1
    return ",".join(f"{word}:{count}" for word, count in word_counts.items())

def index_file(file_name, file_path, cleaned_content, db_connection):
    """
    Indexes the file content and stores it in the database.

    Args:
        file_name: The name of the file.
        file_path: The relative path to the file.
        cleaned_content: The cleaned email content.
        db_connection: A connection to the PostgreSQL database.
    """
    try:
        word_index = create_word_index(cleaned_content)
        cursor = db_connection.cursor()
        cursor.execute(
            "INSERT INTO emails (file_name, file_path, content, word_index) VALUES (%s, %s, %s, %s)",
            (file_name, file_path, cleaned_content, word_index),
        )
        db_connection.commit()
        print(f"Indexed {file_path}")
    except Exception as e:
        print(f"Error indexing {file_path}: {e}")

def process_file(file_path, cleaned_folder, db_connection):
    """
    Processes a cleaned email file and indexes it.

    Args:
        file_path: The path to the cleaned email file.
        cleaned_folder: The path to the folder containing cleaned emails.
        db_connection: A connection to the PostgreSQL database.
    """
    if file_path.endswith(".txt"):
        relative_path = os.path.relpath(file_path, cleaned_folder)
        with open(file_path, 'r', encoding='utf-8') as f:
            cleaned_content = f.read()
        index_file(os.path.basename(file_path), relative_path, cleaned_content, db_connection)

class FileChangeHandler(FileSystemEventHandler):
    def __init__(self, cleaned_folder, db_connection):
        self.cleaned_folder = cleaned_folder
        self.db_connection = db_connection

    def on_created(self, event):
        if not event.is_directory:
            process_file(event.src_path, self.cleaned_folder, self.db_connection)

if __name__ == "__main__":
    cleaned_folder = "litmus_cleaned_mails"

    try:
        db_connection = psycopg2.connect(host=DB_HOST, database=DB_NAME, user=DB_USER, password=DB_PASS)
        print("Connected to PostgreSQL")
    except Exception as e:
        print(f"Error connecting to PostgreSQL: {e}")
        exit(1)

    event_handler = FileChangeHandler(cleaned_folder, db_connection)
    observer = Observer()
    observer.schedule(event_handler, cleaned_folder, recursive=True)
    observer.start()

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
        db_connection.close()
    observer.join()