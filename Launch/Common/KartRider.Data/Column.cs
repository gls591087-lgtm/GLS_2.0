namespace KartRider.Data;

internal class Column
{
	public string Name { get; private set; }

	public bool IsPrimaryKey { get; private set; }

	public bool IsUniqueKey { get; private set; }

	public string ColumnType { get; private set; }

	public Column(dynamic datum)
	{
		Name = datum.COLUMN_NAME;
		IsPrimaryKey = datum.COLUMN_KEY == "PRI";
		IsUniqueKey = datum.COLUMN_KEY == "UNI";
		ColumnType = datum.COLUMN_TYPE;
	}
}
