SELECT	int8, int16, int32, int64, float, datetime, string_1, string_2
FROM	stress_test
WHERE
		string_1 = @string_1
	OR	string_2 = @string_2;