﻿using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Taffy.Lib {
  internal class FileIndexer : IDisposable {
    const LuceneVersion VER = LuceneVersion.LUCENE_48;

    internal string? FolderPath { get; set; }

    internal string? IndexPath => !string.IsNullOrWhiteSpace(FolderPath) ? Path.Combine(FolderPath, ".taffy", "index") : null;

    LuceneDirectory _dir;
    IndexWriter _writer;
    IndexReader _reader;
    IndexSearcher _searcher;

    QueryParser _nameQueryParser;

    internal IndexWriter Writer => _writer;
    internal IndexSearcher Searcher => _searcher;

    internal FileIndexer(string path, bool reset = false) {
      FolderPath = path;
      var analyzer = new CJKAnalyzer(VER);
      var config = new IndexWriterConfig(VER, analyzer);
      config.OpenMode = reset ? OpenMode.CREATE : OpenMode.CREATE_OR_APPEND;

      _dir = Open();
      _writer = new IndexWriter(_dir, config);
      _reader = _writer.GetReader(true);
      _searcher = new IndexSearcher(_reader);

      _nameQueryParser = new QueryParser(VER, "name", analyzer);
    }

    ~FileIndexer() {
      Dispose();
    }

    LuceneDirectory Open() {
      if (IndexPath == null)
        throw new InvalidOperationException("must specify a folder to index.");
      return FSDirectory.Open(IndexPath);
    }

    public void Dispose() {
      _reader.Dispose();
      _writer.Dispose();
      _dir.Dispose();
    }

    public void Commit() {
      Writer.Commit();
    }

    public Query ParseQuery(params string[] terms) {
      var query = string.Join(" ", terms);
      return _nameQueryParser.Parse(query);
    }
  }
}
