#!/usr/bin/env python3
"""Сквозная проверка API: лид -> сделка -> процесс -> задача -> завершение.

Ловит регрессии, которые не видит компилятор: сломанную регистрацию зависимостей,
потерянные доменные события, зависший токен процесса.
"""
import json
import sys
import time
import urllib.error
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080"


def call(method, path, body=None):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(
        BASE + path, data=data, method=method, headers={"Content-Type": "application/json"}
    )
    with urllib.request.urlopen(request, timeout=15) as response:
        raw = response.read().decode()
        return json.loads(raw) if raw else None


def wait_for_api(seconds=90):
    for _ in range(seconds):
        try:
            call("GET", "/api/stages")
            return True
        except (urllib.error.URLError, OSError):
            time.sleep(1)
    return False


def check(condition, message):
    if not condition:
        print(f"ПРОВАЛ: {message}")
        sys.exit(1)
    print(f"ок: {message}")


if not wait_for_api():
    print("ПРОВАЛ: API не поднялся")
    sys.exit(1)

stages = call("GET", "/api/stages")
check(len(stages) >= 2, f"стадии воронки засеяны ({len(stages)} шт.)")

lead = call("POST", "/api/leads", {
    "title": "Smoke-лид",
    "contactName": "Проверка",
    "companyName": "CI",
    "phone": None,
    "email": None,
    "estimatedAmount": 100000,
})
check(lead["status"] == "New", "лид создан")

time.sleep(2)
started = [i for i in call("GET", "/api/processes") if i["subjectEntityId"] == lead["id"]]
check(len(started) == 1, "создание лида запустило процесс через outbox")

opportunity = call("POST", f"/api/leads/{lead['id']}/qualify")
check(opportunity["stageId"] == stages[0]["id"], "лид квалифицирован в сделку на первой стадии")

call("POST", f"/api/opportunities/{opportunity['id']}/stage", {"stageId": stages[1]["id"]})
time.sleep(2)

tasks = [t for t in call("GET", "/api/tasks") if t["subjectEntityId"] == opportunity["id"]]
check(len(tasks) == 1, "смена стадии поставила задачу пользователю")

call("POST", f"/api/tasks/{tasks[0]['id']}/complete", {"result": {"approved": True}})
time.sleep(1)

instance = next(i for i in call("GET", "/api/processes") if i["id"] == tasks[0]["processInstanceId"])
check(instance["status"] == "Completed", "процесс завершился после согласования")

log = [e["elementId"] for e in call("GET", f"/api/processes/{instance['id']}/log")]
check("approved" in log, "процесс прошёл по ветке согласования")

print("\nСквозной сценарий пройден.")
