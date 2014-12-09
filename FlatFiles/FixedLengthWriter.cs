﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using FlatFiles.Properties;

namespace FlatFiles
{
    /// <summary>
    /// Builds textual representations of data by giving each field a fixed width.
    /// </summary>
    public sealed class FixedLengthWriter : IWriter
    {
        private readonly StreamWriter writer;
        private readonly bool ownsStream;
        private readonly FixedLengthSchema schema;
        private readonly FixedLengthOptions options;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of a FixedLengthBuilder.
        /// </summary>
        /// <param name="fileName">The name of the file to write the output to.</param>
        /// <param name="schema">The schema to use to build the output.</param>
        public FixedLengthWriter(string fileName, FixedLengthSchema schema)
            : this(File.OpenWrite(fileName), schema, new FixedLengthOptions(), true)
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthBuilder.
        /// </summary>
        /// <param name="fileName">The name of the file to write the output to.</param>
        /// <param name="schema">The schema to use to build the output.</param>
        /// <param name="options">The options used to format the output.</param>
        public FixedLengthWriter(string fileName, FixedLengthSchema schema, FixedLengthOptions options)
            : this(File.OpenWrite(fileName), schema, options, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthBuilder.
        /// </summary>
        /// <param name="stream">The stream to write the output to.</param>
        /// <param name="schema">The schema to use to build the output.</param>
        public FixedLengthWriter(Stream stream, FixedLengthSchema schema)
            : this(stream, schema, new FixedLengthOptions(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthBuilder.
        /// </summary>
        /// <param name="stream">The stream to write the output to.</param>
        /// <param name="schema">The schema to use to build the output.</param>
        /// <param name="options">The options used to format the output.</param>
        public FixedLengthWriter(Stream stream, FixedLengthSchema schema, FixedLengthOptions options)
            : this(stream, schema, options, false)
        {
        }

        private FixedLengthWriter(Stream stream, FixedLengthSchema schema, FixedLengthOptions options, bool ownsStream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            this.writer = new StreamWriter(stream, options.Encoding ?? Encoding.Default);
            this.schema = schema;
            this.ownsStream = ownsStream;
            this.options = options.Clone();
        }

        /// <summary>
        /// Finalizes the FixedLengthBuilder.
        /// </summary>
        ~FixedLengthWriter()
        {
            dispose(false);
        }

        /// <summary>
        /// Releases any resources currently held by the parser.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private void dispose(bool disposing)
        {
            if (disposing)
            {
                if (ownsStream)
                {
                    writer.Dispose();
                }
                else
                {
                    writer.Flush();
                }
            }
            isDisposed = true;
        }

        /// <summary>
        /// Gets the schema used to build the output.
        /// </summary>
        /// <returns>The schema used to build the output.</returns>
        public ISchema GetSchema()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("FixedLengthWriter");
            }
            return schema;
        }

        /// <summary>
        /// Writes the textual representation of the given values to the writer.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <exception cref="System.ArgumentNullException">The values array is null.</exception>
        public void Write(object[] values)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("FixedLengthWriter");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Length != schema.ColumnDefinitions.Count)
            {
                throw new ArgumentException(Resources.WrongNumberOfValues, "values");
            }
            foreach (string column in schema.FormatValues(values).Select((v, i) => fitWidth(i, v)))
            {
                writer.Write(column);
                if (!string.IsNullOrEmpty(options.ColumnSeparator))
                    writer.Write(options.ColumnSeparator);
            }
            writer.Write(options.RecordSeparator);
        }

        private string fitWidth(int columnIndex, string value)
        {
            if (value == null)
            {
                value = String.Empty;
            }
            Window window = schema.Windows[columnIndex];
            if (value.Length > window.Width)
            {
                int start = value.Length - window.Width;  // take characters on the end
                return value.Substring(start, window.Width);
            }
            else if (value.Length < window.Width)
            {
                if (window.Alignment == FixedAlignment.LeftAligned)
                {
                    return value.PadRight(window.Width, window.FillCharacter ?? options.FillCharacter);
                }
                else
                {
                    return value.PadLeft(window.Width, window.FillCharacter ?? options.FillCharacter);
                }
            }
            else
            {
                return value;
            }
        }
    }
}
