CREATE TABLE stress_test
(
	int8		UInt8 				NOT NULL,
	int16		Int16				NOT NULL,
	int32		Int32				NOT NULL,
	int64		Int64				NOT NULL,
	float		Float32				NOT NULL,
	datetime	Date				NOT NULL,
	string_1	String				NOT NULL,
	string_2	String				NOT NULL
)
	ENGINE = MergeTree()
	PRIMARY KEY (float);