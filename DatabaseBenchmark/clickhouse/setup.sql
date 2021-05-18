CREATE TABLE stress_test
(
	int8_1		UInt8 				NOT NULL,
	int8_2		UInt8 				NOT NULL,
	int16_1		Int16				NOT NULL,
	int16_2		Int16				NOT NULL,
	int32_1		Int32				NOT NULL,
	int32_2		Int32				NOT NULL,
	int64_1		Int64				NOT NULL,
	int64_2		Int64				NOT NULL,
	float_1		Float32				NOT NULL,
	float_2		Float32				NOT NULL,
	datetime_1	Date				NOT NULL,
	datetime_2	Date				NOT NULL,
	string_1	String				NOT NULL,
	string_2	String				NOT NULL
)
	ENGINE = MergeTree()
	PRIMARY KEY (float_1, string_1, string_2);