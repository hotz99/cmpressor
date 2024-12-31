<script>
  export let form;
</script>

<h1>Sign In</h1>

{#if form?.error}
  <p style="color: red;">{form.error.message}</p>
{/if}

<form method="POST">
  <label>
    Email:
    <input type="email" name="email" required />
  </label>
  <br />
  <label>
    Password:
    <input type="password" name="password" required />
  </label>
  <br />
  <button type="submit">Sign In</button>
</form>

{#if form?.success}
  <p style="color: green;">Sign-in successful! Token: {form.token}</p>
{/if}
