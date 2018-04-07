package main

import (
	"container/heap"
	"fmt"
	"io/ioutil"
	"net/http"
	"strconv"
)

type Job struct {
	ID        []byte
	Path      []byte
	Minimum   float64
	heapIndex int
}

type JobPriorityQueue []*Job

func (queue JobPriorityQueue) Len() int { return len(queue) }

func (queue JobPriorityQueue) Less(i, j int) bool {
	return queue[i].Minimum < queue[j].Minimum
}

func (queue JobPriorityQueue) Swap(i, j int) {
	queue[i], queue[j] = queue[j], queue[i]
	queue[i].heapIndex = i
	queue[j].heapIndex = j
}

func (queue *JobPriorityQueue) Push(x interface{}) {
	n := len(*queue)
	item := x.(*Job)
	item.heapIndex = n
	*queue = append(*queue, item)
}

func (queue *JobPriorityQueue) Pop() interface{} {
	old := *queue
	n := len(old)
	item := old[n-1]
	item.heapIndex = -1
	*queue = old[0 : n-1]
	return item
}

var neighbourMatrix []byte
var jobQueue JobPriorityQueue
var id int = 1

func main() {
	// read in the neighbourMatrix
	neighbourMatrix, _ = ioutil.ReadFile("./matrix.txt")
	heap.Push(&jobQueue, &Job{
		ID:      []byte(strconv.Itoa(id)),
		Path:    []byte("0"),
		Minimum: 0,
	})

	http.HandleFunc("/", indexRequestHandler)
	http.HandleFunc("/init/", initRequestHandler)
	http.HandleFunc("/submit/", submitRequestHandler)
	http.HandleFunc("/job/", jobRequestHandler)

	http.ListenAndServe(":80", nil)
}

func indexRequestHandler(w http.ResponseWriter, r *http.Request) {
	w.Write([]byte("ok"))
}

func initRequestHandler(w http.ResponseWriter, r *http.Request) {
	w.Write(neighbourMatrix)
}

func submitRequestHandler(w http.ResponseWriter, r *http.Request) {
	r.ParseForm()
	minimum, _ := strconv.ParseFloat(r.Form["min"][0], 32)
	id++
	submittedJob := &Job{
		ID:      []byte(strconv.Itoa(id)),
		Path:    []byte(r.Form["path"][0]),
		Minimum: minimum,
	}
	heap.Push(&jobQueue, submittedJob)

	w.Write(submittedJob.ID)
}

func jobRequestHandler(w http.ResponseWriter, r *http.Request) {
	if len(jobQueue) > 0 {
		jobToProcess := *heap.Pop(&jobQueue).(*Job)
		w.Write(jobToProcess.ID)
		w.Write([]byte(";"))
		w.Write(jobToProcess.Path)
		w.Write([]byte(";"))
		w.Write([]byte(strconv.FormatFloat(jobToProcess.Minimum, 'f', -1, 32)))
		fmt.Println("job requested " + string(jobToProcess.ID))
	} else {
		w.Write([]byte("*"))
	}
}
