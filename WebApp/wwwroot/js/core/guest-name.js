const GuestNameEngine = (function () {

    const asianSurname = [
        "wang", "li", "zhang", "liu", "chen", "yang", "zhao", "huang",
        "wu", "zhou", "xu", "sun", "ma", "hu", "guo", "lin",
        "kim", "lee", "park", "choi", "jung", "kang",
        "yamada", "tanaka", "sato", "suzuki", "takahashi"
    ];

    const vnCompoundSurname = [
        "au duong", "duong van", "nguyen huu", "nguyen thi", "pham thi"
    ];

    const vietnameseSurname = [
        "nguyen", "tran", "le", "pham", "hoang", "phan", "vu", "vo",
        "dang", "bui", "do", "ho", "ngo", "duong", "ly"
    ];

    const westernSurname = [
        "smith", "johnson", "williams", "brown", "jones", "garcia", "miller", "davis",
        "rodriguez", "martinez", "hernandez", "lopez", "gonzalez", "wilson", "anderson",
        "thomas", "taylor", "moore", "jackson", "martin", "lee", "perez", "thompson",
        "white", "harris", "sanchez", "clark", "ramirez", "lewis", "robinson", "walker",
        "young", "allen", "king", "wright", "scott", "torres", "nguyen", "hill", "flores",
        "green", "adams", "nelson", "baker", "hall", "rivera", "campbell", "mitchell",
        "carter", "roberts", "clinton", "bush", "obama", "trump", "biden"
    ];

    function normalizeName(name) {
        if (!name) return "";

        return name
            .normalize("NFC")
            .replace(/\s+/g, " ")
            .trim();
    }

    function smartCapitalize(name) {
        if (!name) return "";

        return name
            .split(" ")
            .map(w => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
            .join(" ");
    }

    function detectVietnamese(name) {

        let firstWord = name.split(" ")[0].toLowerCase();

        if (vietnameseSurname.includes(firstWord))
            return true;

        return /[ăâđêôơưáàạảãấầẩẫậắằẳẵặ]/i.test(name);
    }

    function detectWestern(name) {
        let words = name.split(" ");
        let lastWord = words[words.length - 1].toLowerCase();
        return westernSurname.includes(lastWord);
    }

    function detectAsian(name) {
        return /[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff]/.test(name);
    }

    function splitWestern(name) {
        if (name.includes(",")) {
            let parts = name.split(",");
            let remain = parts[0].trim().split(" "); 
            let first = parts[1].trim();            

            return {
                first: first,
                middle: remain.length > 1 ? remain.slice(1).join(" ") : "",
                last: remain[0]
            };
        }

        let arr = name.split(" ");
        if (arr.length === 1) return { first: arr[0], middle: "", last: "" };
        if (arr.length === 2) return { first: arr[0], middle: "", last: arr[1] };

        return {
            first: arr[0],
            middle: arr.slice(1, arr.length - 1).join(" "),
            last: arr[arr.length - 1]
        };
    }

    function splitVietnamese(name) {
        if (name.includes(",")) {
            let parts = name.split(",");
            let first = parts[1].trim();
            let remain = parts[0].trim().split(" ");

            return {
                first: first,
                middle: remain.length > 1 ? remain.slice(1).join(" ") : "",
                last: remain[0]
            };
        }

        // Logic mặc định không dấu phẩy
        let arr = name.split(" ");
        if (arr.length === 1) return { first: arr[0], middle: "", last: "" };
        let last = arr[0];
        let first = arr[arr.length - 1];
        let middle = arr.slice(1, arr.length - 1).join(" ");
        return { first, middle, last };
    }

    function splitAsian(name) {
        if (name.includes(",")) {
            let parts = name.split(",");
            return {
                last: parts[0].trim(),
                first: parts[1].trim(),
                middle: ""
            };
        }

        if (name.length <= 1) return { first: name, middle: "", last: "" };
        return { last: name.charAt(0), first: name.slice(1), middle: "" };
    }

    function parseFullName(name) {
        name = normalizeName(name);
        if (!name) return { first: "", middle: "", last: "" };

        if (detectAsian(name)) return splitAsian(name);

        if (detectVietnamese(name)) return splitVietnamese(name);

        if (detectWestern(name)) return splitWestern(name);

        return splitWestern(name);
    }

    function buildFullName(first, middle, last) {

        first = normalizeName(first);
        middle = normalizeName(middle);
        last = normalizeName(last);

        if (!first && !middle && !last) return "";

        let name = [last, middle, first]
            .filter(x => x)
            .join(" ");

        return smartCapitalize(name);
    }

    function buildSalutation(first, last = "") {
        first = normalizeName(first);
        if (!first) return "";

        return "Dear " + smartCapitalize(first);
    }

    return {
        parseFullName,
        buildFullName,
        buildSalutation,
        smartCapitalize
    };

})();