public class BadWordFilter
{
    private List<string> badWords = new List<string> {  "elma", "Elma", "ELMA", "e lma", "e  lma", "e l m a", "e-lma", "e_lma", "e|lma", "@lma", "3lma", "e1ma", "e1m4", "e|ma", "e!ma", "e lmaaaa",
"e.lma", "el.ma", "elm.a", "elmaaaa", "e|1m@", "3|m@", "e+lma", "e~lma", "e@lma", "el.m.a", "el.maaa", "e*lm@",

"armut", "Armut", "ARMUT", "a rmut", "a-rmut", "@rmut", "4rmut", "ar_mu7", "a  r  m  u  t", "ar m-ut", "armu7", "ar.mut", "ar|mut",
"a~rmut", "a rm u t", "armutttt", "a|r|m|u|t", "ar,m,u,t", "arm.ut", "a_rmu7", "arm@ut", "a+rmut", "ar.mu.t", "armutttt", "a!rm!ut",

"çilek", "Çilek", "ÇİLEK", "ç i lek", "c ilek", "c!lek", "ç ilekkkkk", "c_ilek", "ç_i_le_k", "@ilek", "ç ilek",
"ç.i.lek", "ci1ek", "ç|lek", "ç*ilek", "ç!lek", "ç-i-lek", "ç1lek", "çi_lek", "ç.i.le.k", "ç_il_ek", "ç+ilek", "ç.i.lekkkk", "ç!lekkkk",

"kiraz", "Kiraz", "KİRAZ", "ki raz", "k!raz", "k1raz", "k_r@z", "k_r a z", "kir a zzzz", "k*ra*z", "ki!raz",
"kira.z", "ki|raz", "k1r4z", "kir@@z", "k_r-a-z", "k.ir.az", "k!r.az", "k1.ra.z", "kir~az", "k~i~r~a~z", "ki.ra.zzz", "kir+az",

"üzüm", "Üzüm", "ÜZÜM", "u züm", "u_züm", "uZ m", "u  Z  ü m", "u_z_um", "@züüm", "u$um", "u_zü_m", "u~zum",
"uz.um", "uz|um", "u~zu~m", "u zuuum", "uz|u|m", "uzzzum", "u+zum", "ü.z.üm", "uz_um", "ü$üm", "üzümüm", "u_zü_mm",

"portakal", "Portakal", "PORTAKAL", "P  ortakal", "P-o-r-t-a-k-a-l", "P@rtakal", "P0rt4k4l", "po rt akal", "portakal...", "por|takal",
"p+ortakal", "p_o_rt_a_k_a_l", "port@kal", "p0rtakal", "portakaaal", "po~rta~kal", "p*ortak@l", "p0r.ta.kal", "por!takal", "po_rtakal",

"mandalina", "Mandalina", "MANDALİNA", "m a n d a l i n a", "m a n d 4 l i n a", "m@nd@lina", "m4nd4lin4", "mand ali na", "manda|lina",
"m~anda~lina", "m!and!al!ina", "mandalinaa", "manda1ina", "mand4lin@", "man~da~lina", "mand~alina", "m+a+n+d+a+l+i+n+a", "mand@lin@", "m4nda|lina",

"karpuz", "Karpuz", "KARPUZ", "kar puz", "k a r p u z", "k a r p u z z z", "k!rp uz", "k r p u z", "k@rpu z", "k4r p uz", "k-r-pu-z",
"kar.puz", "k|arpuz", "kar+pu+z", "karpuuuz", "k@rpuz", "k@r.puz", "k+r.puz", "k-r.puz", "kar~puz", "karpuzz", "kar+pu+zz",

"kavun", "Kavun", "KAVUN", "k@v un", "K 4 v un", "k avvvvunnn", "k_av  un", "k-a-v-u-n", "k4v u n", "ka~vun",
"ka.v.un", "ka|vun", "k~avun", "kavunnn", "ka!v!un", "k+avun", "k~a~vun", "k~a~v~u~n", "k4vu~n", "k@vun", "ka~vun~", "ka~v.u~n",

"şeftali", "Şeftali", "ŞEFTALİ", "ş ef tali", "s e f t a l i", "ş ef+ali", "ş eft ali", "ş e f t a l i", "s!ef+ali", "ş~ef+tali",
"ş.e.f.t.a.l.i", "s+eftali", "s|eftali", "s3ftali", "s#eftali", "ş+e+f+t+a+l+i", "s~ef+tali", "şefta~li", "ş.ef.ta.li", "ş~e~ftali",

"ayva", "Ayva", "AYVA", "a y va", "4 y v a", "a y-v a", "a!yva", "ayvaa", "ay!va", "a_yv@",
"ayv+a", "a~yv~a", "ay.v.a", "ay~va", "ayvvva", "ay~v+a", "a.yv+va", "ay.vvv.a", "ay!v!a", "ayva~",

"ananas", "Ananas", "ANANAS", "a n a n as", "@nan@s", "a|n4n4s", "anan as", "anan @s", "anan.a.s", "ana~nas",
"an.an.as", "an+a+n+a+s", "ananasss", "anan@s", "anan~as", "an+a+n+a+s", "an!anas", "anan.a.s.s", "an~an~as", "anan+ass",

"vişne", "Vişne", "VİŞNE", "v i ş n e", "v i s n e", "vi$ne", "v issne", "vis!ne", "v+i+s+n+e", "v+isne",
"v~is~ne", "v.i.s.n.e", "visssne", "viss.ne", "v+i+şn+e", "v~i~ş~ne", "vi~sn~e", "vis.n.e",

"erik", "Erik", "ERİK", "€ r i k", "e r rrik", "er!ik", "e.r.i.k", "e!rik", "e|rik", "e+rik", "eriikkk",
"e~ri~k", "e!r.i.k", "er+ik", "erik~", "erikkk", "e+ri+k", "e+ri~k", "e.ri.k", "e.r.i.k.k", "er!i!k",

"greyfurt", "Greyfurt", "GREYFURT", "g r ey fu r t", "gr ey furt", "gr€y f urt", "g rey + furt", "g~reyfurt", "g~r~ey~furt",
"grey!furt", "g.re.y.fu.rt", "grey-furt", "greyfuurt", "g.r.e.y.f.u.r.t", "grey+furt", "g~rey+furt", "gr3y~furt", "g~r~fu~rt"
   }; // Küfürlü kelimeler

    // Mesajda küfürlü kelime olup olmadığını kontrol et
    public bool ContainsBadWord(string message)
    {
        foreach (var word in badWords)
        {
            if (message.ToLower().Contains(word.ToLower()))
            {
                return true;
            }
        }
        return false;
    }
}
