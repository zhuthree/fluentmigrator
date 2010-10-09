#region License
// 
// Copyright (c) 2007-2009, Sean Chambers <schambers80@gmail.com>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FluentMigrator.Expressions;
using FluentMigrator.Model;

namespace FluentMigrator.Runner.Generators
{
	public abstract class GeneratorBase : IMigrationGenerator
	{
		private readonly ITypeMap _typeMap;
		private IConstantFormatter _constant = new ConstantFormatter();

		public GeneratorBase(ITypeMap typeMap)
		{
			_typeMap = typeMap;
		}
		
		protected string GetTypeMap(DbType type, int size, int precision)
		{
			return _typeMap.GetTypeMap(type, size, precision);
		}

		public abstract string Generate(CreateSchemaExpression expression);
		public abstract string Generate(DeleteSchemaExpression expression);
		public abstract string Generate(CreateTableExpression expression);
		public abstract string Generate(AlterColumnExpression expression);
		public abstract string Generate(CreateColumnExpression expression);
		public abstract string Generate(DeleteTableExpression expression);
		public abstract string Generate(DeleteColumnExpression expression);
		public abstract string Generate(CreateForeignKeyExpression expression);
		public abstract string Generate(DeleteForeignKeyExpression expression);
		public abstract string Generate(CreateIndexExpression expression);
		public abstract string Generate(DeleteIndexExpression expression);
		public abstract string Generate(RenameTableExpression expression);
		public abstract string Generate(RenameColumnExpression expression);
		public abstract string Generate(InsertDataExpression expression);
		public abstract string Generate(AlterDefaultConstraintExpression expression);
	    public abstract string Generate(DeleteDataExpression expression);

		protected virtual string GenerateDDLForColumn(ColumnDefinition column)
		{
			var sb = new StringBuilder();

			sb.Append(column.Name);
			sb.Append(" ");

			if (column.Type.HasValue)
			{
				sb.Append(GetTypeMap(column.Type.Value, column.Size, column.Precision));
			}
			else
			{
				sb.Append(column.CustomType);
			}

			if (!column.IsNullable)
			{
				sb.Append(" NOT NULL");
			}

			if (column.DefaultValue != null)
			{
				sb.Append(" DEFAULT ");
				sb.Append(Constant.Format(column.DefaultValue));
			}

			if (column.IsIdentity)
			{
				sb.Append(" IDENTITY(1,1)");
			}

			if (column.IsPrimaryKey)
			{
				sb.Append(" PRIMARY KEY CLUSTERED");
			}

			return sb.ToString();
		}

		protected string GetColumnDDL(CreateTableExpression expression)
		{
			IList<ColumnDefinition> columns = expression.Columns;
			string result = "";
			int total = columns.Count - 1;

			//if more than one column is a primary key or the primary key is given a name, then it needs to be added separately
			IList<ColumnDefinition> primaryKeyColumns = GetPrimaryKeyColumns(columns);
			bool addPrimaryKeySeparately = false;
			if (primaryKeyColumns.Count > 1
				|| (primaryKeyColumns.Count == 1 && primaryKeyColumns[0].PrimaryKeyName != null))
			{
				addPrimaryKeySeparately = true;
				foreach (ColumnDefinition column in primaryKeyColumns)
				{
					column.IsPrimaryKey = false;
				}
			}

			for (int i = 0; i < columns.Count; i++)
			{
				result += GenerateDDLForColumn(columns[i]);

				if (i != total)
					result += ", ";
			}

			if (addPrimaryKeySeparately)
				result = AddPrimaryKeyConstraint(expression.TableName, primaryKeyColumns, result);

			return result;
		}

		private IList<ColumnDefinition> GetPrimaryKeyColumns(IList<ColumnDefinition> columns)
		{
			IList<ColumnDefinition> primaryKeyColumns = new List<ColumnDefinition>();
			foreach (ColumnDefinition column in columns)
			{
				if (column.IsPrimaryKey)
				{
					primaryKeyColumns.Add(column);
				}
			}
			return primaryKeyColumns;
		}

		private string AddPrimaryKeyConstraint(string tableName, IList<ColumnDefinition> primaryKeyColumns, string result)
		{
			string keyColumns = "";
			foreach (ColumnDefinition column in primaryKeyColumns)
			{
				keyColumns += column.Name + ",";
			}
			keyColumns = keyColumns.TrimEnd(',');
			result += String.Format(", {0} PRIMARY KEY ({1})", GetPrimaryKeyConstraintName(primaryKeyColumns, tableName), keyColumns);

			return result;
		}

		/// <summary>
		/// Gets the name of the primary key constraint. Some Generators may need to override if the constraint name is limited
		/// </summary>
		/// <returns></returns>
		protected virtual string GetPrimaryKeyConstraintName(IList<ColumnDefinition> primaryKeyColumns, string tableName)
		{
			string keyName = string.Empty;
			string assignedName = string.Empty;
			foreach (ColumnDefinition column in primaryKeyColumns)
			{
				keyName += column.Name + "_";

				if (!string.IsNullOrEmpty(column.PrimaryKeyName))
				{
					assignedName = column.PrimaryKeyName;
					break; // no need to keep going
				}
			}

			keyName += "PK";

			if (!string.IsNullOrEmpty(assignedName))
			{
				keyName = assignedName;
			}

			return string.Format("CONSTRAINT {0}", keyName);
		}

		protected IConstantFormatter Constant
		{
			get { return _constant; }
		}
	}
}