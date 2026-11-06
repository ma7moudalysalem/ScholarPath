"""Write Arabic titles and descriptions for the imported real listings.

    python -I translate_ar.py <listings.tsv> <out.sql>

The DAAD, Erasmus Mundus and government listings are imported with English text
only. This script machine-translates each title and description into Arabic with
the Azure OpenAI deployment named in AZ_OAI_TRANSLATE (default gpt-6.1-sol), so
the Arabic side of the knowledge base describes them too. Facts are not changed:
the model is told to translate, keep names of programmes and institutions
recognisable, and add nothing. The output is a SQL script of UPDATEs.

listings.tsv: Id<TAB>TitleEn<TAB>DescriptionEn, UTF-8, one listing per line.
Needs AZ_OAI_ENDPOINT and AZ_OAI_KEY.
"""
import json
import os
import sys
import time
import urllib.request

ENDPOINT = os.environ["AZ_OAI_ENDPOINT"].rstrip("/")
KEY = os.environ["AZ_OAI_KEY"]
DEP = os.environ.get("AZ_OAI_TRANSLATE", "gpt-6.1-sol")
URL = f"{ENDPOINT}/openai/deployments/{DEP}/chat/completions?api-version=2025-04-01-preview"
SYSTEM = ("Translate each scholarship title and description from English to Modern Standard Arabic. "
          "Keep programme, university and organisation names recognisable (transliterate or keep the "
          "Latin name in brackets). Do not add, remove or change any fact. Reply ONLY with JSON "
          '{"items":[{"id":"...","title":"...","description":"..."}]} in the same order.')


def call(batch):
    body = json.dumps({"messages": [{"role": "system", "content": SYSTEM},
                                    {"role": "user", "content": json.dumps(batch, ensure_ascii=False)}],
                       "max_completion_tokens": 16000,
                       "response_format": {"type": "json_object"}}).encode()
    for attempt in range(5):
        req = urllib.request.Request(URL, data=body, headers={"api-key": KEY, "Content-Type": "application/json"})
        try:
            r = json.load(urllib.request.urlopen(req, timeout=300))
            return json.loads(r["choices"][0]["message"]["content"])["items"]
        except urllib.error.HTTPError as e:
            if e.code != 429:
                raise
            time.sleep(10 * (attempt + 1))
    raise RuntimeError("rate limited")


def q(s):
    return "N'" + s.replace("'", "''") + "'"


def main():
    rows = [line.rstrip("\n").split("\t") for line in open(sys.argv[1], encoding="utf-8") if line.strip()]
    out = ["SET NOCOUNT ON;"]
    for i in range(0, len(rows), 20):
        batch = [{"id": r[0], "title": r[1], "description": r[2]} for r in rows[i:i + 20]]
        got = {x["id"]: x for x in call(batch)}
        for r in rows[i:i + 20]:
            t = got.get(r[0])
            if t:
                out.append(f"UPDATE Scholarships SET TitleAr={q(t['title'])}, DescriptionAr={q(t['description'])} WHERE Id='{r[0]}';")
        print(f"{min(i + 20, len(rows))}/{len(rows)}", flush=True)
    open(sys.argv[2], "w", encoding="utf-8").write("\n".join(out))


if __name__ == "__main__":
    main()
