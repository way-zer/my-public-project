package cf.wayzer

import com.mpatric.mp3agic.Mp3File
import io.ktor.client.HttpClient
import io.ktor.client.call.call
import io.ktor.client.response.readBytes
import java.io.File
import java.net.URLEncoder
import java.nio.charset.Charset
import java.text.SimpleDateFormat
import java.util.*
import java.util.logging.Logger

data class Page(
    val name: String,
    val date: Date,
    val tag:String
) {
    suspend fun download(){
//        return
        val dir = File("voa/"+ dateFormat.format(date))
        dir.mkdirs()

        //http://www.voase.cn/2018/11/2018-11-20%20[Health%20and%20Lifestyle]%20Mushroom%20Hunting%20Gains%20Popularity%20in%20US.txt
        var urlP = cf.wayzer.HOMEPAGE+ urlDateFormat.format(date)+ dateFormat.format(date)+" [$tag] "+
                URLEncoder.encode(name, Charset.defaultCharset()).replace("+"," ")
        Logger.getGlobal().info("[Download Start] ${dateFormat.format(date)} | $name")
        client.call("$urlP.mp3").response.readBytes().let {
            if(it.size<1024*500){
                Logger.getGlobal().warning("[Download Fail] ${dateFormat.format(date)} | $name")
                return
            }
            val file = File(dir,"[Temp]$name.mp3")
            file.writeBytes(it)
            val mp3file = Mp3File(file)
            mp3file.id3v2Tag.let { mp3tag ->
                mp3tag.title=name
                mp3tag.artist="Voa"
                mp3tag.album= dateFormat.format(date)
                mp3tag.key = this.tag
            }
            mp3file.save(File(dir,"[V]$name.mp3").absolutePath)
            file.delete()
        }
        client.call("$urlP.txt").response.readBytes().let { File(dir,"[T]$name.txt").writeBytes(it) }
        Logger.getGlobal().info("[Download End] ${dateFormat.format(date)} | $name")
    }
    companion object {
        lateinit var client:HttpClient
        val dateFormat = SimpleDateFormat("yyyy-MM-dd")
        val urlDateFormat = SimpleDateFormat("yyyy/MM/")
        fun readPage(url:String,title:String):Page{
            val rawUrl = url.split("/")[3].replace(".html","")
            val date = dateFormat.parse(rawUrl.split("-[")[0])
            val tag = rawUrl.substringAfter("-[").substringBefore("]-").replace("-"," ")
            val name = title.substringAfter(" ").replace(Regex(" [^x00-xff'].*"),"")
            return Page(name,date,tag)
        }
    }
}
