package main

import (
	"database/sql"
	"math/rand"
	"os"
	"strconv"
	"strings"

	_ "github.com/mattn/go-sqlite3"
)

func main() {

	// open input file
	fi, err := os.Create("Neurons.sql")
	if err != nil {
		panic(err)
	}
	// close fi on exit and check for its returned error
	defer func() {
		if err := fi.Close(); err != nil {
			panic(err)
		}
	}()

	db, err := sql.Open("sqlite3", "NeuronsF1000.db")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	query := `
    CREATE TABLE IF NOT EXISTS NEURONS (
        id     INTEGER PRIMARY KEY
                       NOT NULL,
        neuron INTEGER NOT NULL,
        typen  CHAR    NOT NULL
    );
    CREATE TABLE IF NOT EXISTS AXONS (
        id     INTEGER PRIMARY KEY AUTOINCREMENT,
        neuron INTEGER NOT NULL,
        axon   INTEGER NOT NULL,
        i      INTEGER NOT NULL,
        j      INTEGER NOT NULL,
        k      INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS SYNAPSES (
        id      INTEGER PRIMARY KEY AUTOINCREMENT,
        neuron  INTEGER NOT NULL,
        synapse INTEGER NOT NULL,
        i       INTEGER NOT NULL,
        j       INTEGER NOT NULL,
        k       INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS DNA (
        id       INTEGER PRIMARY KEY
                         UNIQUE
                         NOT NULL,
        neuron   INTEGER NOT NULL,
        pattern  INTEGER NOT NULL,
        response INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS CA (
        rule   STRING  NOT NULL,
        lenght INTEGER NOT NULL,
        height INTEGER NOT NULL,
        width  INTEGER NOT NULL
	);
	`

	_, err = db.Exec(query)
	if err != nil {
		panic(err)
	}

	fi.WriteString(query)

	//добавляем КА
	lenght := 100
	height := 50
	width := 50
	query = "INSERT INTO CA (rule, lenght, height, width) VALUES ('26 [0]/13/40', 100, 50, 50);"
	_, err = db.Exec(query)
	if err != nil {
		panic(err)
	}

	fi.WriteString(query)

	//добавляем нейроны
	query = "INSERT INTO NEURONS (neuron, typen) VALUES "
	for i := 1000; i < 1500; i++ {
		query += "(" + strconv.Itoa(i) + ", " + "'c'), "
	}
	for i := 1500; i < 2000; i++ {
		query += "(" + strconv.Itoa(i) + ", " + "'s'), "
	}
	query = strings.TrimSuffix(query, ", ")

	_, err = db.Exec(query)
	if err != nil {
		panic(err)
	}
	fi.WriteString(query + ";")

	query = "INSERT INTO SYNAPSES (neuron, synapse, i,j,k) VALUES "
	query2 := "INSERT INTO AXONS (neuron, axon, i,j,k) VALUES "
	//сначала нейроны, слушающие микрофон
	//все их выходы придут во вторую секцию, ИНДРИИ
	///////////////////////////////////////////////////////////////////////////////////////////////////////
	//пройдемся сначала по структурным нейронам, а потом повторим все то же самое для суммирующих
	//реализуем принцип степенной функции везде; сначала делаем нейроны, с недалеко удаленными синапсами
	for i := 1000; i < 1050; i++ {
		syni := randomInt(0, lenght)
		synj := randomInt(0, height) //вокруг этой точки, на небольшом расстоянии, располагаются все синапсы
		for j := 0; j < 6; j++ {     //пока только по 6 синапсов у этих нейронов
			synii := syni + randomInt(-7, 7)
			synjj := synj + randomInt(-7, 7)
			if synii < 0 {
				synii = 0
			} else if synii > lenght-1 {
				synii = lenght - 1
			}
			if synjj < 0 {
				synjj = 0
			} else if synjj > height-1 {
				synjj = height - 1
			}

			//на левой стенке
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(synii) + ", " +
				strconv.Itoa(synjj) + ", " +
				strconv.Itoa(0) + "), "
			//и на правой стенке
			query += "(" + strconv.Itoa(i+50) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(synii) + ", " +
				strconv.Itoa(synjj) + ", " +
				strconv.Itoa(width-1) + "), "
		}
		//и один аксон, стреляющий в ИНДРИИ
		query2 += "(" + strconv.Itoa(i) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
		query2 += "(" + strconv.Itoa(i+50) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
	}
	//полностью случайные синапсы на аудиовходе
	for i := 1100; i < 1150; i++ {
		for j := 0; j < 6; j++ { //пока только по 6 синапсов у этих нейронов
			//на левой стенке
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(0, 100)) + ", " +
				strconv.Itoa(randomInt(0, 50)) + ", " +
				strconv.Itoa(0) + "), "
			//и на правой стенке
			query += "(" + strconv.Itoa(i+50) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(0, 100)) + ", " +
				strconv.Itoa(randomInt(0, 50)) + ", " +
				strconv.Itoa(width-1) + "), "
		}
		//и один аксон, стреляющий в ИНДРИИ
		query2 += "(" + strconv.Itoa(i) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
		query2 += "(" + strconv.Itoa(i+50) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
	}
	//гиперсвязанные нейроны в каждой секции по 1 гиперграфу
	query += queryValHyperSynBy10(0, 11, 1200) + queryValHyperSynBy10(10, 21, 1210) + queryValHyperSynBy10(20, 31, 1220) + queryValHyperSynBy10(30, 41, 1230) +
		queryValHyperSynBy10(40, 51, 1240) + queryValHyperSynBy10(50, 61, 1250) + queryValHyperSynBy10(60, 71, 1260) + queryValHyperSynBy10(70, 81, 1270) +
		queryValHyperSynBy10(80, 91, 1280) + queryValHyperSynBy10(90, 100, 1290) //299 нейрон
	query2 += queryValHyperAxBy10(0, 11, 1200) + queryValHyperAxBy10(10, 21, 1210) + queryValHyperAxBy10(20, 31, 1220) + queryValHyperAxBy10(30, 41, 1230) +
		queryValHyperAxBy10(40, 51, 1240) + queryValHyperAxBy10(50, 61, 1250) + queryValHyperAxBy10(60, 71, 1260) + queryValHyperAxBy10(70, 81, 1270) +
		queryValHyperAxBy10(80, 91, 1280) + queryValHyperAxBy10(90, 100, 1290) //299 нейрон

	//гиперсвязанные нейроны через все секции
	q1, q2 := queryValHyperBy10Full(1300)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1310)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1320)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1330)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1340)
	query += q1
	query2 += q2

	//тупо случайные нейроны
	for i := 1350; i < 1500; i++ {
		for j := 0; j < 6; j++ {
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(1, 99)) + ", " +
				strconv.Itoa(randomInt(1, 49)) + ", " +
				strconv.Itoa(randomInt(1, 49)) + "), "
		}
		query2 += "(" + strconv.Itoa(i) + ", 0, " +
			strconv.Itoa(randomInt(1, 99)) + ", " +
			strconv.Itoa(randomInt(1, 49)) + ", " +
			strconv.Itoa(randomInt(1, 49)) + "), "
	}
	query = strings.TrimSuffix(query, ", ")
	query2 = strings.TrimSuffix(query2, ", ")
	_, err = db.Exec(query)
	if err != nil {
		panic(err)
	}
	_, err = db.Exec(query2)
	if err != nil {
		panic(err)
	}
	fi.WriteString(query + ";")
	fi.WriteString(query2 + ";")

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//и то же самое для суммирующих нейронов
	query = "INSERT INTO SYNAPSES (neuron, synapse, i,j,k) VALUES "
	query2 = "INSERT INTO AXONS (neuron, axon, i,j,k) VALUES "
	//сначала нейроны, слушающие микрофон
	//все их выходы придут во вторую секцию, ИНДРИИ
	///////////////////////////////////////////////////////////////////////////////////////////////////////
	//пройдемся сначала по структурным нейронам, а потом повторим все то же самое для суммирующих
	//реализуем принцип степенной функции везде; сначала делаем нейроны, с недалеко удаленными синапсами
	for i := 1500; i < 1550; i++ {
		syni := randomInt(0, lenght)
		synj := randomInt(0, height) //вокруг этой точки, на небольшом расстоянии, располагаются все синапсы
		for j := 0; j < 6; j++ {     //пока только по 6 синапсов у этих нейронов
			synii := syni + randomInt(-7, 7)
			synjj := synj + randomInt(-7, 7)
			if synii < 0 {
				synii = 0
			} else if synii > lenght-1 {
				synii = lenght - 1
			}
			if synjj < 0 {
				synjj = 0
			} else if synjj > height-1 {
				synjj = height - 1
			}

			//на левой стенке
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(synii) + ", " +
				strconv.Itoa(synjj) + ", " +
				strconv.Itoa(0) + "), "
			//и на правой стенке
			query += "(" + strconv.Itoa(i+50) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(synii) + ", " +
				strconv.Itoa(synjj) + ", " +
				strconv.Itoa(width-1) + "), "
		}
		//и один аксон, стреляющий в ИНДРИИ
		query2 += "(" + strconv.Itoa(i) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
		query2 += "(" + strconv.Itoa(i+50) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
	}
	//полностью случайные синапсы на аудиовходе
	for i := 1600; i < 1650; i++ {
		for j := 0; j < 6; j++ { //пока только по 6 синапсов у этих нейронов
			//на левой стенке
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(0, 100)) + ", " +
				strconv.Itoa(randomInt(0, 50)) + ", " +
				strconv.Itoa(0) + "), "
			//и на правой стенке
			query += "(" + strconv.Itoa(i+50) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(0, 100)) + ", " +
				strconv.Itoa(randomInt(0, 50)) + ", " +
				strconv.Itoa(width-1) + "), "
		}
		//и один аксон, стреляющий в ИНДРИИ
		query2 += "(" + strconv.Itoa(i) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
		query2 += "(" + strconv.Itoa(i+50) + ", " +
			strconv.Itoa(0) + ", " +
			strconv.Itoa(randomInt(10, 21)) + ", " +
			strconv.Itoa(randomInt(1, height-1)) + ", " +
			strconv.Itoa(randomInt(1, width-1)) + "), "
	}
	//гиперсвязанные нейроны в каждой секции по 1 гиперграфу
	query += queryValHyperSynBy10(0, 11, 1700) + queryValHyperSynBy10(10, 21, 1710) + queryValHyperSynBy10(20, 31, 1720) + queryValHyperSynBy10(30, 41, 1730) +
		queryValHyperSynBy10(40, 51, 1740) + queryValHyperSynBy10(50, 61, 1750) + queryValHyperSynBy10(60, 71, 1760) + queryValHyperSynBy10(70, 81, 1770) +
		queryValHyperSynBy10(80, 91, 1780) + queryValHyperSynBy10(90, 100, 1790) //299 нейрон
	query2 += queryValHyperAxBy10(0, 11, 1700) + queryValHyperAxBy10(10, 21, 1710) + queryValHyperAxBy10(20, 31, 1720) + queryValHyperAxBy10(30, 41, 1730) +
		queryValHyperAxBy10(40, 51, 1740) + queryValHyperAxBy10(50, 61, 1750) + queryValHyperAxBy10(60, 71, 1760) + queryValHyperAxBy10(70, 81, 1770) +
		queryValHyperAxBy10(80, 91, 1780) + queryValHyperAxBy10(90, 100, 1790) //299 нейрон

	//гиперсвязанные нейроны через все секции
	q1, q2 = queryValHyperBy10Full(1800)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1810)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1820)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1830)
	query += q1
	query2 += q2
	q1, q2 = queryValHyperBy10Full(1840)
	query += q1
	query2 += q2

	//тупо случайные нейроны
	for i := 1850; i < 2000; i++ {
		for j := 0; j < 6; j++ {
			query += "(" + strconv.Itoa(i) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(randomInt(1, 99)) + ", " +
				strconv.Itoa(randomInt(1, 49)) + ", " +
				strconv.Itoa(randomInt(1, 49)) + "), "
		}
		query2 += "(" + strconv.Itoa(i) + ", 0, " +
			strconv.Itoa(randomInt(1, 99)) + ", " +
			strconv.Itoa(randomInt(1, 49)) + ", " +
			strconv.Itoa(randomInt(1, 49)) + "), "
	}
	query = strings.TrimSuffix(query, ", ")
	query2 = strings.TrimSuffix(query2, ", ")

	_, err = db.Exec(query)
	if err != nil {
		panic(err)
	}
	_, err = db.Exec(query2)
	if err != nil {
		panic(err)
	}
	fi.WriteString(query + ";")
	fi.WriteString(query2 + ";")
}

type coo struct {
	i, j, k int
}

func queryValHyperBy10Full(startNeuron int) (string, string) {
	query := ""
	query2 := ""
	var syns [10][9]coo
	for i := 0; i < 10; i++ {
		for j := 0; j < 9; j++ {
			syns[i][j] = coo{randomInt(i*10, (i+1)*10), randomInt(0, 50), randomInt(0, 50)}
			query += "(" + strconv.Itoa(i+startNeuron) + ", " + //neuron number
				strconv.Itoa(j) + ", " + //synapse number
				strconv.Itoa(syns[i][j].i) + ", " + //i
				strconv.Itoa(syns[i][j].j) + ", " + //j
				strconv.Itoa(syns[i][j].k) + "), " //k

			if j == 0 && i < 9 {

				query2 += "(" + strconv.Itoa(i+startNeuron) + ", " + //neuron number
					strconv.Itoa(j) + ", " + //synapse number
					strconv.Itoa(syns[i][j].i) + ", " + //i
					strconv.Itoa(syns[i][j].j) + ", " + //j
					strconv.Itoa(syns[i][j].k) + "), " //k
			}

		}
	}
	for i := 2; i < 10; i++ {
		for j := 1; j < i; j++ {
			query2 += "(" + strconv.Itoa(i+startNeuron) + "," + strconv.Itoa(j) + "," + strconv.Itoa(syns[j-1][i-j].i) +
				"," + strconv.Itoa(syns[j-1][i-j].j) + "," + strconv.Itoa(syns[j-1][i-j].k) + "), "
		}
	}

	return query, query2
}

// Returns an int >= min, < max
func randomInt(min, max int) int {
	return min + rand.Intn(max-min)
}

func queryValHyperSynBy10(left, right, startNeuron int) string {
	//mid:=right-left
	query := ""
	for i := 0; i < 10; i++ {
		query += "(" + strconv.Itoa(i+startNeuron) + ", " + //neuron number
			strconv.Itoa(0) + ", " + //synapse number
			strconv.Itoa(left) + ", " + //i
			strconv.Itoa(10+i) + ", " + //j
			strconv.Itoa(30) + "), " //k
		for j := 1; j < 9; j++ {
			query += "(" + strconv.Itoa(i+startNeuron) + ", " +
				strconv.Itoa(j) + ", " +
				strconv.Itoa(left+j) + ", " +
				strconv.Itoa(10+i) + ", " +
				strconv.Itoa(30) + "), "
		}
	}
	return query
}

func queryValHyperAxBy10(left, right, startNeuron int) string {
	query := ""
	//вперед
	for i := 0; i < 9; i++ {
		query += "(" + strconv.Itoa(i+startNeuron) + ", " + //neuron number
			strconv.Itoa(0) + ", " + //ax number
			strconv.Itoa(left) + ", " + //i
			strconv.Itoa(11+i) + ", " + //j
			strconv.Itoa(30) + "), " //k
	}
	//назад
	query += "(" + strconv.Itoa(2+startNeuron) + ",1," + strconv.Itoa(left+1) + ",10,30), " +

		"(" + strconv.Itoa(3+startNeuron) + ",1," + strconv.Itoa(left+2) + ",10,30), " +
		"(" + strconv.Itoa(3+startNeuron) + ",2," + strconv.Itoa(left+1) + ",11,30), " +

		"(" + strconv.Itoa(4+startNeuron) + ",1," + strconv.Itoa(left+3) + ",10,30), " +
		"(" + strconv.Itoa(4+startNeuron) + ",2," + strconv.Itoa(left+2) + ",11,30), " +
		"(" + strconv.Itoa(4+startNeuron) + ",3," + strconv.Itoa(left+1) + ",12,30), " +

		"(" + strconv.Itoa(5+startNeuron) + ",1," + strconv.Itoa(left+4) + ",10,30), " +
		"(" + strconv.Itoa(5+startNeuron) + ",2," + strconv.Itoa(left+3) + ",11,30), " +
		"(" + strconv.Itoa(5+startNeuron) + ",3," + strconv.Itoa(left+2) + ",12,30), " +
		"(" + strconv.Itoa(5+startNeuron) + ",4," + strconv.Itoa(left+1) + ",13,30), " +

		"(" + strconv.Itoa(6+startNeuron) + ",1," + strconv.Itoa(left+5) + ",10,30), " +
		"(" + strconv.Itoa(6+startNeuron) + ",2," + strconv.Itoa(left+4) + ",11,30), " +
		"(" + strconv.Itoa(6+startNeuron) + ",3," + strconv.Itoa(left+3) + ",12,30), " +
		"(" + strconv.Itoa(6+startNeuron) + ",4," + strconv.Itoa(left+2) + ",13,30), " +
		"(" + strconv.Itoa(6+startNeuron) + ",5," + strconv.Itoa(left+1) + ",14,30), " +

		"(" + strconv.Itoa(7+startNeuron) + ",1," + strconv.Itoa(left+6) + ",10,30), " +
		"(" + strconv.Itoa(7+startNeuron) + ",2," + strconv.Itoa(left+5) + ",11,30), " +
		"(" + strconv.Itoa(7+startNeuron) + ",3," + strconv.Itoa(left+4) + ",12,30), " +
		"(" + strconv.Itoa(7+startNeuron) + ",4," + strconv.Itoa(left+3) + ",13,30), " +
		"(" + strconv.Itoa(7+startNeuron) + ",5," + strconv.Itoa(left+2) + ",14,30), " +
		"(" + strconv.Itoa(7+startNeuron) + ",6," + strconv.Itoa(left+1) + ",15,30), " +

		"(" + strconv.Itoa(8+startNeuron) + ",1," + strconv.Itoa(left+7) + ",10,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",2," + strconv.Itoa(left+6) + ",11,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",3," + strconv.Itoa(left+5) + ",12,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",4," + strconv.Itoa(left+4) + ",13,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",5," + strconv.Itoa(left+3) + ",14,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",6," + strconv.Itoa(left+2) + ",15,30), " +
		"(" + strconv.Itoa(8+startNeuron) + ",7," + strconv.Itoa(left+1) + ",16,30), " +

		"(" + strconv.Itoa(9+startNeuron) + ",1," + strconv.Itoa(left+8) + ",10,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",2," + strconv.Itoa(left+7) + ",11,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",3," + strconv.Itoa(left+6) + ",12,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",4," + strconv.Itoa(left+5) + ",13,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",5," + strconv.Itoa(left+4) + ",14,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",6," + strconv.Itoa(left+3) + ",15,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",7," + strconv.Itoa(left+2) + ",16,30), " +
		"(" + strconv.Itoa(9+startNeuron) + ",8," + strconv.Itoa(left+1) + ",17,30), "

	return query
}
