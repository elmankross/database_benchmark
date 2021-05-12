set encoding utf8
set title "Dynamic of the request times"
set xlabel "Iterations"
set ylabel "Elapsed milliseconds"
set grid
plot ARG1 using 1 with lines title "Select", \
	 ARG1 using 2 with lines title "Insert One", \
	 ARG1 using 3 with lines title "Insert Many"
pause mouse close