<package main

import (
	"log"
	"net/http"
	"os"
	"strings"
)

func main() {
	if _, err := os.Stat("./.log"); os.IsNotExist(err) {
		os.Create("./.log")
	}
	f, err := os.OpenFile("./.log", os.O_APPEND|os.O_WRONLY, 0600)
	if err != nil {
		panic(err)
	}

	defer f.Close()
	log.SetOutput(f)

	http.HandleFunc("/", pageRequestHandler)
	http.HandleFunc("/css/", contentRequestHandler)
	http.HandleFunc("/img/", contentRequestHandler)
	http.HandleFunc("/src/", contentRequestHandler)
	http.ListenAndServe(":80", nil)
}

func pageRequestHandler(w http.ResponseWriter, r *http.Request) {
	log.Println(r.RemoteAddr + " requested " + r.RequestURI)
	if strings.HasSuffix(r.RequestURI, "/") {
		ServeFileIfExisting(w, r, "./html/"+strings.TrimRight(r.RequestURI, "/")+".html")
	} else {
		ServeFileIfExisting(w, r, "./html/"+r.RequestURI+".html")
	}
}

func contentRequestHandler(w http.ResponseWriter, r *http.Request) {
	log.Println(r.RemoteAddr + " requested " + r.RequestURI)
	ServeFileIfExisting(w, r, "."+r.RequestURI)
}

func ServeFileIfExisting(w http.ResponseWriter, r *http.Request, file string) {
	if _, err := os.Stat(file); os.IsNotExist(err) {
		http.ServeFile(w, r, "./html/404.html")
	} else {
		http.ServeFile(w, r, file)
	}
}
