package cf.wayzer

import io.ktor.client.*
import io.ktor.client.engine.cio.*
import io.ktor.client.request.get
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.runBlocking
import org.jsoup.Jsoup
import java.io.File
import java.util.*

const val HOMEPAGE = "http://www.voase.cn/"
fun main(args: Array<String>){
    val client = HttpClient(CIO)
    runBlocking{
        Page.client=client
        val home = client.get<String>(HOMEPAGE)
        val dateFile = File("voa/lastDate")
        val lastDate = if(dateFile.exists())
            dateFile.readText().let { Page.dateFormat.parse(it) }
        else Page.dateFormat.parse("2018-01-01")

        Jsoup.parse(home)
            .select("#listall ul li a +a")
            .map { Page.readPage(it.attr("href"), it.text())}
            .filter { it.date.after(lastDate) }
            .map { async{
                it.download()
            } }
            .awaitAll()
        dateFile.writeText(Page.dateFormat.format(Calendar.getInstance().time))
    }
    client.close()
}


