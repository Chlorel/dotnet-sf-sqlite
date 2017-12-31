﻿using LoadDllAsEmbeddedRes;
using SFSQLiteApi.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SFSQLiteApi
{
    public class SFSQLite
    {
        #region Properties

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        private SFSQLiteConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        private string Database { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SFSQLite"/> class.
        /// </summary>
        /// <param name="db">The database.</param>
        public SFSQLite(string db)
        {
            this.Database = db;
        }

        #endregion Constructors

        #region Public Static Methods

        /// <summary>
        /// Inicializa API
        /// </summary>
        public static void InitializeApi()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceList = assembly.GetManifestResourceNames();

            foreach (string resource in resourceList)
            {
                if (resource.Contains(Constant.SQLiteDll))
                {
                    EmbeddedAssembly.Load(resource, Constant.SQLiteDll);
                    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                }
                else
                {
                    string fileName = string.Empty;
                    Stream stream = assembly.GetManifestResourceStream(resource);

                    if (resource.Contains(Constant.x64))
                    {
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constant.x64);

                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }

                        fileName = Path.Combine(filePath, Constant.SQLiteInteropDll);
                    }
                    else if (resource.Contains(Constant.x86))
                    {
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constant.x86);

                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }

                        fileName = Path.Combine(filePath, Constant.SQLiteInteropDll);
                    }

                    if (!File.Exists(fileName))
                    {
                        using (Stream file = File.Create(fileName))
                        {
                            Utility.CopyStream(stream, file);
                        }
                    }
                }
            }
        }

        #endregion Public Static Methods

        #region Public Methods

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void CloseConnection()
        {
            this.Connection.CloseDbConnection();
            this.Connection = null;
        }

        /// <summary>
        /// Creates the table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CreateTable<T>()
        {
            if (this.Connection != null)
            {
                this.Connection.CreateTable<T>();
            }
            else
            {
                this.ThrowConnectionException();
            }
        }

        /// <summary>
        /// Deletes the row.
        /// </summary>
        /// <param name="deleteObj">The delete object.</param>
        /// <returns></returns>
        public int DeleteRow(object deleteObj)
        {
            if (this.Connection != null)
            {
                return (this.Connection.DeleteRow(deleteObj));
            }
            else
            {
                this.ThrowConnectionException();
                return (-1);
            }
        }

        /// <summary>
        /// Gets the column maximum value.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        public object GetColumnMaxValue<T>(string columnName)
        {
            if (this.Connection != null)
            {
                return (this.Connection.GetColumnMaxValue<T>(columnName));
            }
            else
            {
                this.ThrowConnectionException();
                return null;
            }
        }

        /// <summary>
        /// Gets the rows total.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public int GetRowsTotal<T>(string whereClause = "") where T : new()
        {
            if (this.Connection != null)
            {
                return (this.Connection.GetRowsTotal<T>(whereClause));
            }
            else
            {
                this.ThrowConnectionException();
                return (-1);
            }
        }

        /// <summary>
        /// Inserts the row.
        /// </summary>
        /// <param name="insertObj">The insert object.</param>
        /// <returns></returns>
        public int InsertRow(object insertObj)
        {
            if (this.Connection != null)
            {
                return (this.Connection.InsertRow(insertObj));
            }
            else
            {
                this.ThrowConnectionException();
                return (-1);
            }
        }

        /// <summary>
        /// Opens the connection.
        /// </summary>
        public void OpenConnection()
        {
            this.Connection = new SFSQLiteConnection(this.Database);
        }

        /// <summary>
        /// Selects all rows.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public List<T> SelectAllRows<T>(string whereClause = "") where T : new()
        {
            if (this.Connection != null)
            {
                return (this.Connection.SelectAllRows<T>(whereClause));
            }
            else
            {
                this.ThrowConnectionException();
                return null;
            }
        }

        /// <summary>
        /// Selects the one row.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public T SelectOneRow<T>(string whereClause = "") where T : new()
        {
            if (this.Connection != null)
            {
                return (this.Connection.SelectOneRow<T>(whereClause));
            }
            else
            {
                this.ThrowConnectionException();
                return new T();
            }
        }

        /// <summary>
        /// Updates the row.
        /// </summary>
        /// <param name="updateObj">The update object.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public int UpdateRow(object updateObj, string whereClause = "")
        {
            if (this.Connection != null)
            {
                return (this.Connection.UpdateRow(updateObj, whereClause));
            }
            else
            {
                this.ThrowConnectionException();
                return (-1);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handles the AssemblyResolve event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }

        /// <summary>
        /// Throws the connection exception.
        /// </summary>
        /// <exception cref="System.Exception">Necessary create a SFSQLiteConnection. Use method OpenConnection().</exception>
        private void ThrowConnectionException()
        {
            throw new Exception("Necessary create a SFSQLiteConnection. Use method OpenConnection().");
        }

        #endregion Private Methods
    }
}