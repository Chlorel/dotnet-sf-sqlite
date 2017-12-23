﻿using SFSQLiteApi.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SFSQLiteApi
{
    internal class SFSQLiteConnection
    {
        #region Members

        private SQLiteConnection Connection { get; set; }

        #endregion Members

        #region Constructors

        public SFSQLiteConnection(string db)
        {
            //Create database if not exists
            this.CreateDatabase(db);
            //Connects to database
            this.OpenDbConnection(db);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Termina ligação à base de dados
        /// </summary>
        /// <returns></returns>
        public bool CloseDbConnection()
        {
            bool result = true;

            try
            {
                if (this.Connection.State != ConnectionState.Closed)
                {
                    this.Connection.Close();
                }
            }
            catch (Exception exception)
            {
                SFLog.WriteError(this, "CloseDbConnection", exception.Message);
                result = false;
            }
            finally
            {
                this.Connection.Dispose();
                this.Connection = null;
            }

            return result;
        }

        /// <summary>
        /// Cria uma nova tabela na base de dados
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CreateTable<T>()
        {
            try
            {
                int aux = 1;
                List<string> keyColumnList = new List<string>();

                var objectType = typeof(T);
                var propertyList = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                int propertiesCount = propertyList.Length;

                StringBuilder sbCreateTable = new StringBuilder();
                sbCreateTable.Append("CREATE TABLE IF NOT EXISTS ");
                sbCreateTable.Append(objectType.Name);
                sbCreateTable.Append("(");

                foreach (var property in propertyList)
                {
                    if (property.IsDataMember())
                    {
                        if (property.IsKey())
                        {
                            keyColumnList.Add(property.Name);
                        }

                        sbCreateTable.Append(property.Name);
                        sbCreateTable.Append(" ");
                        sbCreateTable.Append(property.PropertyType.ToSQLiteDataType());

                        if (property.PropertyType.IsNull())
                        {
                            sbCreateTable.Append(" NULL");
                        }
                        else
                        {
                            sbCreateTable.Append(" NOT NULL");
                        }

                        sbCreateTable.Append(",");
                    }
                }

                if (keyColumnList.Count > 0)
                {
                    sbCreateTable.Append("PRIMARY KEY (");

                    foreach (string key in keyColumnList)
                    {
                        sbCreateTable.Append(key);

                        if (aux < keyColumnList.Count)
                        {
                            sbCreateTable.Append(",");
                        }

                        aux++;
                    }

                    sbCreateTable.Append("));");
                }
                else
                {
                    sbCreateTable.Remove((sbCreateTable.Length - 1), 1);
                    sbCreateTable.Append(");");
                }

                SQLiteQuery.ExecuteNonQuery(sbCreateTable.ToString(), this.Connection);
            }
            catch (Exception exception)
            {
                SFLog.WriteError(this, "CreateTable", exception.Message);
            }
        }

        /// <summary>
        /// Deletes the row.
        /// </summary>
        /// <param name="deleteObj">The delete object.</param>
        /// <returns></returns>
        public int DeleteRow(object deleteObj)
        {
            int aux = 1;
            var keyColumnList = new List<string>();

            var objectType = deleteObj.GetType();
            var propertyList = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            StringBuilder deleteQuery = new StringBuilder();
            deleteQuery.Append("DELETE FROM ");
            deleteQuery.Append(objectType.Name);
            deleteQuery.Append(" WHERE ");
            aux = 1;

            foreach (var property in propertyList.Where(x => x.IsDataMember() && x.IsKey()))
            {
                keyColumnList.Add(property.Name);
            }

            foreach (string key in keyColumnList)
            {
                PropertyInfo property = propertyList.FirstOrDefault(x => x.Name == key);

                if (property != null)
                {
                    deleteQuery.Append(key);
                    deleteQuery.Append("=");
                    deleteQuery.Append("'");
                    deleteQuery.Append(property.GetValue(deleteObj, null));
                    deleteQuery.Append("'");
                }

                if (aux < keyColumnList.Count)
                {
                    deleteQuery.Append(" AND ");
                }

                aux++;
            }

            return (SQLiteQuery.ExecuteNonQuery(deleteQuery.ToString(), this.Connection));
        }

        /// <summary>
        /// Retorna total de linhas da tabela do objeto T com base no where (caso não seja vazio)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public ulong GetRowsTotal<T>(string whereClause = "") where T : new()
        {
            ulong returnValue = 0;
            SQLiteDataReader reader = null;

            try
            {
                var objectType = typeof(T);
                var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                string sqlQuery = SQLiteQuery.CountTotal(objectType.Name, whereClause);
                reader = SQLiteQuery.ExecuteReader(sqlQuery, this.Connection);

                while (reader.Read())
                {
                    returnValue = Convert.ToUInt64(reader[Constant.TotalCount]);
                }
            }
            catch (Exception exception)
            {
                SFLog.ConsoleWriteLine(exception.Message);
            }
            finally
            {
                reader.Close();
                reader = null;
            }

            return returnValue;
        }

        /// <summary>
        /// Insere um novo registo na tabela do tipo do objeto passado como parametro
        /// </summary>
        /// <param name="insertObj"></param>
        /// <returns></returns>
        public int InsertRow(object insertObj)
        {
            int aux = 1;
            var keyColumnList = new List<string>();
            var byteArrayColumnList = new List<string>();
            var byteArrayList = new List<byte[]>();

            var objectType = insertObj.GetType();
            var propertyList = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int propertiesCount = propertyList.Length;

            StringBuilder insertQuery = new StringBuilder();
            insertQuery.Append("INSERT INTO ");
            insertQuery.Append(objectType.Name);
            insertQuery.Append("(");

            foreach (var property in propertyList)
            {
                if (property.IsDataMember())
                {
                    insertQuery.Append(property.Name);

                    if (aux < propertiesCount)
                    {
                        insertQuery.Append(",");
                    }

                    aux++;
                }
            }

            if (insertQuery.ToString().EndsWith(","))
            {
                insertQuery.Remove((insertQuery.Length - 1), 1);
            }

            insertQuery.Append(") VALUES(");
            aux = 1;

            foreach (var property in propertyList)
            {
                if (property.IsDataMember())
                {
                    object value = property.GetValue(insertObj, null);
                    value = Utility.GetValue(byteArrayColumnList, byteArrayList, property, value);

                    if (value == null)
                    {
                        insertQuery.Append("NULL");
                    }
                    else
                    {
                        insertQuery.Append("'");
                        insertQuery.Append(value.ToString());
                        insertQuery.Append("'");
                    }

                    if (aux < propertiesCount)
                    {
                        insertQuery.Append(",");
                    }
                }

                aux++;
            }

            if (insertQuery.ToString().EndsWith(","))
            {
                insertQuery.Remove((insertQuery.Length - 1), 1);
            }

            insertQuery.Append(")");

            int result = SQLiteQuery.ExecuteNonQuery(insertQuery.ToString(), this.Connection);

            if (result > 0)
            {
                this.HandleByteArrayList(objectType.Name, byteArrayColumnList, byteArrayList);
            }

            return result;
        }

        /// <summary>
        /// Retorna lista de objetos T após um SELECT à base de dados
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public List<T> SelectAllRows<T>(string whereClause = "") where T : new()
        {
            var objectList = new List<T>();
            SQLiteDataReader reader = null;

            try
            {
                var objectType = typeof(T);
                var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                string sqlQuery = SQLiteQuery.SelectAllFrom(objectType.Name, whereClause);
                reader = SQLiteQuery.ExecuteReader(sqlQuery, this.Connection);

                while (reader.Read())
                {
                    var newObject = new T();

                    foreach (var property in properties)
                    {
                        if (property.IsDataMember())
                        {
                            if (reader[property.Name].HaveContent())
                            {
                                Type type = property.PropertyType;
                                TypeExtension.GetDataType(type, out type);

                                object propertyValue = reader[property.Name];
                                dynamic changedObject = Convert.ChangeType(propertyValue, type);
                                property.SetValue(newObject, changedObject, null);
                            }
                        }
                    }

                    objectList.Add(newObject);
                }
            }
            catch (Exception exception)
            {
                SFLog.ConsoleWriteLine(exception.Message);
            }
            finally
            {
                reader.Close();
                reader = null;
            }

            return objectList;
        }

        /// <summary>
        /// Retorna um objeto T após um SELECT à base de dados
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public T SelectOneRow<T>(string whereClause = "") where T : new()
        {
            var objectList = this.SelectAllRows<T>(whereClause);

            return (objectList.FirstOrDefault());
        }

        /// <summary>
        /// Atualiza registo na tabela do tipo do objeto passado como parametro
        /// </summary>
        /// <param name="updateObj"></param>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public int UpdateRow(object updateObj, string whereClause = "")
        {
            int aux = 1;
            var keyColumnList = new List<string>();
            var byteArrayColumnList = new List<string>();
            var byteArrayList = new List<byte[]>();

            var objectType = updateObj.GetType();
            var propertyList = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int propertiesCount = propertyList.Length;

            StringBuilder updateQuery = new StringBuilder();
            updateQuery.Append("UPDATE ");
            updateQuery.Append(objectType.Name);
            updateQuery.Append(" SET ");

            foreach (var property in propertyList)
            {
                if (property.IsDataMember())
                {
                    if (property.IsKey())
                    {
                        keyColumnList.Add(property.Name);
                    }

                    updateQuery.Append(property.Name);
                    updateQuery.Append("=");

                    object value = property.GetValue(updateObj, null);
                    value = Utility.GetValue(byteArrayColumnList, byteArrayList, property, value);

                    if (value == null)
                    {
                        updateQuery.Append("NULL");
                    }
                    else
                    {
                        updateQuery.Append("'");
                        updateQuery.Append(value.ToString());
                        updateQuery.Append("'");
                    }

                    if (aux < propertiesCount)
                    {
                        updateQuery.Append(",");
                    }
                }

                aux++;
            }

            if (updateQuery.ToString().EndsWith(","))
            {
                updateQuery.Remove((updateQuery.Length - 1), 1);
            }

            updateQuery.Append(" WHERE ");
            aux = 1;

            if (string.IsNullOrWhiteSpace(whereClause))
            {
                foreach (string key in keyColumnList)
                {
                    PropertyInfo property = propertyList.FirstOrDefault(x => x.Name == key);

                    if (property != null)
                    {
                        updateQuery.Append(key);
                        updateQuery.Append("=");
                        updateQuery.Append("'");
                        updateQuery.Append(property.GetValue(updateObj, null));
                        updateQuery.Append("'");
                    }

                    if (aux < keyColumnList.Count)
                    {
                        updateQuery.Append(" AND ");
                    }

                    aux++;
                }
            }
            else
            {
                updateQuery.Append(whereClause);
            }

            int result = SQLiteQuery.ExecuteNonQuery(updateQuery.ToString(), this.Connection);

            if (result > 0)
            {
                this.HandleByteArrayList(objectType.Name, byteArrayColumnList, byteArrayList);
            }

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Create database if not exists
        /// </summary>
        /// <param name="db"></param>
        private void CreateDatabase(string db)
        {
            db = Path.ChangeExtension(db, Constant.SQLite);

            if (!File.Exists(db))
            {
                SQLiteConnection.CreateFile(db);
            }
        }

        /// <summary>
        /// Inserir/atualizar arrays de bytes na base de dados
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="byteArrayColumnList"></param>
        /// <param name="byteArrayList"></param>
        private void HandleByteArrayList(string tableName, List<string> byteArrayColumnList, List<byte[]> byteArrayList)
        {
            for (int i = 0; i < byteArrayColumnList.Count; i++)
            {
                SQLiteQuery.InsertUpdateByteArray(tableName, byteArrayColumnList[i], byteArrayList[i], this.Connection);
            }
        }

        /// <summary>
        /// Opens a database connection
        /// </summary>
        /// <param name="db"></param>
        private void OpenDbConnection(string db)
        {
            string connectionString = string.Format(StringFormat.ConnectionString, db);
            this.Connection = new SQLiteConnection(connectionString);

            if (this.Connection.State != ConnectionState.Open)
            {
                this.Connection.Open();
            }
        }

        #endregion Private Methods
    }
}