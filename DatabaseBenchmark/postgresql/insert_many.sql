COPY stress_test (	
	int8_1, 
	int8_2, 
	int16_1, 
	int16_2, 
	int32_1, 
	int32_2, 
	int64_1, 
	int64_2, 
	float_1, 
	float_2, 
	datetime_1, 
	datetime_2, 
	string_1, 
	string_2) 
FROM STDIN (FORMAT BINARY);